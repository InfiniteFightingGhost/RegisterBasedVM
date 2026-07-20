using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Raptor.Attributes;

namespace Raptor
{
    /// <summary>
    /// Registry that discovers, validates, and binds Raptor FFI host methods
    /// using reflection on <see cref="RaptorMethodAttribute"/>-decorated methods.
    /// Produces <see cref="VirtualMachine.HostFFIDelegate"/> entries that can be
    /// registered with the VM or ScriptEngine.
    /// </summary>
    public sealed class FFIHostTable
    {
        private readonly Dictionary<
            string,
            (ushort Index, VirtualMachine.HostFFIDelegate Callback)
        > _methods = new();
        private readonly List<RaptorMethodInfo> _methodInfos = new();
        private readonly HashSet<ushort> _usedIndices = new();
        private ushort _nextAutoIndex = 0;
        private readonly Dictionary<ushort, IntPtr> _unmanagedCallbacks = new();

        /// <summary>
        /// All registered methods, keyed by name.
        /// </summary>
        public IReadOnlyDictionary<
            string,
            (ushort Index, VirtualMachine.HostFFIDelegate Callback)
        > Methods => _methods;

        /// <summary>
        /// All registered unmanaged callbacks, mapped by method index.
        /// </summary>
        public IReadOnlyDictionary<ushort, IntPtr> UnmanagedCallbacks => _unmanagedCallbacks;

        /// <summary>
        /// Metadata for all registered methods.
        /// </summary>
        public IReadOnlyList<RaptorMethodInfo> MethodInfos => _methodInfos;

        // --------------------------------------------
        //  Manual Registration
        // --------------------------------------------

        /// <summary>
        /// Manually registers a host method. This is the existing workflow fallback.
        /// </summary>
        public void Register(string name, ushort index, VirtualMachine.HostFFIDelegate callback)
        {
            ValidateNoDuplicateName(name, "manual");
            ValidateNoDuplicateIndex(index, name);
            _methods[name] = (index, callback);
            _usedIndices.Add(index);
            _methodInfos.Add(
                new RaptorMethodInfo(name, index, null, false, null, System.Array.Empty<string>())
            );
            if (callback.Method.IsStatic && callback.Target == null)
            {
                _unmanagedCallbacks[index] = callback.Method.MethodHandle.GetFunctionPointer();
            }
        }

        /// <summary>
        /// Manually registers an unmanaged host method function pointer.
        /// </summary>
        public unsafe void Register(
            string name,
            ushort index,
            delegate* unmanaged[Cdecl]<VMState*, void> callback
        )
        {
            ValidateNoDuplicateName(name, "manual_unmanaged");
            ValidateNoDuplicateIndex(index, name);
            _unmanagedCallbacks[index] = (IntPtr)callback;
            _usedIndices.Add(index);
            _methodInfos.Add(
                new RaptorMethodInfo(name, index, null, false, null, System.Array.Empty<string>())
            );
            _methods[name] = (
                index,
                (ref VMState state) =>
                {
                    fixed (VMState* p = &state)
                    {
                        callback(p);
                    }
                }
            );
        }

        // --------------------------------------------
        //  Reflection-Based Registration
        // --------------------------------------------

        /// <summary>
        /// Scans an object instance for <see cref="RaptorMethodAttribute"/>-decorated
        /// methods (both static and instance) and registers them.
        /// </summary>
        [RequiresUnreferencedCode(
            "Instance-based module registration uses reflection to discover methods."
        )]
        public void RegisterModule(object instance)
        {
            var type = instance.GetType();
            var moduleAttr = type.GetCustomAttribute<RaptorModuleAttribute>();
            string? prefix = moduleAttr?.Prefix;

            var methods = type.GetMethods(
                BindingFlags.Public
                    | BindingFlags.Instance
                    | BindingFlags.Static
                    | BindingFlags.DeclaredOnly
            );
            foreach (var method in methods)
            {
                ProcessMethod(method, method.IsStatic ? null : instance, prefix);
            }
        }

        /// <summary>
        /// Scans a type for <see cref="RaptorMethodAttribute"/>-decorated static methods
        /// and registers them.
        /// </summary>
        public void RegisterModule(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type type
        )
        {
            var moduleAttr = type.GetCustomAttribute<RaptorModuleAttribute>();
            string? prefix = moduleAttr?.Prefix;

            var methods = type.GetMethods(
                BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly
            );
            foreach (var method in methods)
            {
                ProcessMethod(method, null, prefix);
            }
        }

        /// <summary>
        /// Scans a type for <see cref="RaptorMethodAttribute"/>-decorated static methods
        /// and registers them.
        /// </summary>
        public void RegisterModule<
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T
        >() => RegisterModule(typeof(T));

        /// <summary>
        /// Creates an <see cref="FFIHostTable"/> from all types in an assembly
        /// that are decorated with <see cref="RaptorModuleAttribute"/>.
        /// </summary>
        [RequiresUnreferencedCode(
            "Assembly scanning uses reflection to discover RaptorModule types."
        )]
        public static FFIHostTable FromAssembly(Assembly assembly)
        {
            var table = new FFIHostTable();
            foreach (var type in assembly.GetTypes())
            {
                if (type.GetCustomAttribute<RaptorModuleAttribute>() != null)
                {
                    table.RegisterModule(type);
                }
            }
            return table;
        }

        // --------------------------------------------
        //  Core Scanning Logic
        // --------------------------------------------

        private void ProcessMethod(MethodInfo method, object? instance, string? prefix)
        {
            if (method.GetCustomAttribute<RaptorIgnoreAttribute>() != null)
                return;

            var raptorAttr = method.GetCustomAttribute<RaptorMethodAttribute>();
            if (raptorAttr == null)
                return;

            string name = raptorAttr.Name ?? ToCamelCase(method.Name);
            if (!string.IsNullOrEmpty(prefix))
                name = prefix + "." + name;

            // Resolve index
            ushort index;
            if (raptorAttr.Index >= 0)
            {
                index = (ushort)raptorAttr.Index;
            }
            else
            {
                index = AllocateNextIndex();
            }

            ValidateNoDuplicateName(name, method.DeclaringType?.Name ?? "unknown");
            ValidateNoDuplicateIndex(index, name);

            VirtualMachine.HostFFIDelegate callback = CreateDelegate(method, instance);

            if (method.IsStatic && IsDirectBindSignature(method))
            {
                _unmanagedCallbacks[index] = method.MethodHandle.GetFunctionPointer();
            }

            var descAttr = method.GetCustomAttribute<RaptorDescriptionAttribute>();
            bool isPure = method.GetCustomAttribute<RaptorPureAttribute>() != null;
            var paramDescs = CollectParameterDescriptions(method);
            var exposedParams = new List<string>();
            foreach (var item in method.GetCustomAttributes<RaptorParamAttribute>())
            {
                if (item.ParamName != null)
                    exposedParams.Add(item.ParamName);
            }
            foreach (var p in method.GetParameters())
            {
                if (p.ParameterType != typeof(VMState).MakeByRefType())
                {
                    exposedParams.Add(p.Name ?? "arg");
                }
            }

            var info = new RaptorMethodInfo(
                name,
                index,
                descAttr?.Description,
                isPure,
                paramDescs,
                exposedParams
            );

            _methods.Add(name, (index, callback));
            _usedIndices.Add(index);
            _methodInfos.Add(info);
        }

        // --------------------------------------------
        //  Delegate Creation (Dual-Path)
        // --------------------------------------------

        private static VirtualMachine.HostFFIDelegate CreateDelegate(
            MethodInfo method,
            object? instance
        )
        {
            // Path 1: Direct bind — void Method(ref VMState state)
            if (IsDirectBindSignature(method))
            {
                if (method.IsStatic)
                {
                    return (VirtualMachine.HostFFIDelegate)
                        Delegate.CreateDelegate(typeof(VirtualMachine.HostFFIDelegate), method);
                }
                else
                {
                    return (VirtualMachine.HostFFIDelegate)
                        Delegate.CreateDelegate(
                            typeof(VirtualMachine.HostFFIDelegate),
                            instance!,
                            method
                        );
                }
            }

            // Path 2: Typed wrapper — other supported signatures
            return CreateTypedWrapper(method, instance);
        }

        private static bool IsDirectBindSignature(MethodInfo method)
        {
            if (method.ReturnType != typeof(void))
                return false;
            var parameters = method.GetParameters();
            return parameters.Length == 1
                && parameters[0].ParameterType == typeof(VMState).MakeByRefType();
        }

        private static VirtualMachine.HostFFIDelegate CreateTypedWrapper(
            MethodInfo method,
            object? instance
        )
        {
            var parameters = method.GetParameters();
            var returnType = method.ReturnType;

            // Try the zero-allocation fast path first
            var fastPath = TryCreateFastPathDelegate(method, instance, parameters, returnType);
            if (fastPath != null)
            {
                System.Console.WriteLine(
                    $"[Raptor FFI Debug] Method '{method.Name}' successfully bound to zero-allocation FAST-PATH delegate."
                );
                return fastPath;
            }

            System.Console.Error.WriteLine(
                $"[Raptor FFI Debug] Method '{method.Name}' fell back to slow REFLECTION path."
            );

            // Separate ref VMState parameters from register-mapped parameters
            bool hasStateParam = false;
            int stateParamIndex = -1;
            var registerParams = new List<(int paramIndex, Type type)>();

            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].ParameterType == typeof(VMState).MakeByRefType())
                {
                    if (hasStateParam)
                        throw new InvalidOperationException(
                            $"Method '{method.DeclaringType?.Name}.{method.Name}' has multiple ref VMState parameters."
                        );
                    hasStateParam = true;
                    stateParamIndex = i;
                }
                else
                {
                    ValidateSupportedType(parameters[i].ParameterType, method, parameters[i].Name!);
                    registerParams.Add((i, parameters[i].ParameterType));
                }
            }

            if (returnType != typeof(void))
                ValidateSupportedReturnType(returnType, method);

            // Build the argument array layout and converters at registration time
            int totalParams = parameters.Length;
            var argReaders = new Func<double, object>[registerParams.Count];
            for (int i = 0; i < registerParams.Count; i++)
            {
                argReaders[i] = CreateArgReader(registerParams[i].type);
            }
            Func<object, double>? returnWriter =
                returnType != typeof(void) ? CreateReturnWriter(returnType) : null;

#if NET8_0_OR_GREATER
            // Create the MethodInvoker once at registration time
            var invoker = System.Reflection.MethodInvoker.Create(method);
#endif

            // Capture everything in a closure
            return (ref VMState state) =>
            {
                unsafe
                {
                    // Rent from ArrayPool to avoid heap allocations
                    object?[] poolArray = System.Buffers.ArrayPool<object?>.Shared.Rent(
                        totalParams
                    );
                    Span<object?> args = poolArray.AsSpan(0, totalParams);
                    try
                    {
                        int regSlot = 0;

                        for (int i = 0; i < totalParams; i++)
                        {
                            if (i == stateParamIndex)
                            {
                                args[i] = null; // placeholder
                            }
                            else
                            {
                                args[i] = argReaders[regSlot](state.RegPtr[regSlot]);
                                regSlot++;
                            }
                        }

                        object? result;
                        if (hasStateParam)
                        {
                            // Use a boxed VMState copy for execution, then copy back
                            object boxedState = state;
                            args[stateParamIndex] = boxedState;
#if NET8_0_OR_GREATER
                            result = invoker.Invoke(instance, args);
#else
                            object?[] invokeArgs = new object?[totalParams];
                            for (int idx = 0; idx < totalParams; idx++)
                                invokeArgs[idx] = args[idx];
                            result = method.Invoke(instance, invokeArgs);
#endif
                            state = (VMState)args[stateParamIndex]!;
                        }
                        else
                        {
#if NET8_0_OR_GREATER
                            result = invoker.Invoke(instance, args);
#else
                            object?[] invokeArgs = new object?[totalParams];
                            for (int idx = 0; idx < totalParams; idx++)
                                invokeArgs[idx] = args[idx];
                            result = method.Invoke(instance, invokeArgs);
#endif
                        }

                        if (returnWriter != null && result != null)
                        {
                            state.RegPtr[0] = returnWriter(result);
                        }
                    }
                    finally
                    {
                        // Clear references to prevent memory leaks in the pool
                        args.Clear();
                        System.Buffers.ArrayPool<object?>.Shared.Return(poolArray);
                    }
                }
            };
        }

        private static VirtualMachine.HostFFIDelegate? TryCreateFastPathDelegate(
            MethodInfo method,
            object? instance,
            ParameterInfo[] parameters,
            Type returnType
        )
        {
            // 1. Check if all parameters are double
            bool allDoubleParams = true;
            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].ParameterType != typeof(double))
                {
                    allDoubleParams = false;
                    break;
                }
            }

            if (allDoubleParams)
            {
                if (returnType == typeof(double))
                {
                    switch (parameters.Length)
                    {
                        case 0:
                            var d0 =
                                (Func<double>)
                                    Delegate.CreateDelegate(typeof(Func<double>), instance, method);
                            return (ref VMState state) =>
                            {
                                unsafe
                                {
                                    state.RegPtr[0] = d0();
                                }
                            };
                        case 1:
                            var d1 =
                                (Func<double, double>)
                                    Delegate.CreateDelegate(
                                        typeof(Func<double, double>),
                                        instance,
                                        method
                                    );
                            return (ref VMState state) =>
                            {
                                unsafe
                                {
                                    state.RegPtr[0] = d1(state.RegPtr[0]);
                                }
                            };
                        case 2:
                            var d2 =
                                (Func<double, double, double>)
                                    Delegate.CreateDelegate(
                                        typeof(Func<double, double, double>),
                                        instance,
                                        method
                                    );
                            return (ref VMState state) =>
                            {
                                unsafe
                                {
                                    state.RegPtr[0] = d2(state.RegPtr[0], state.RegPtr[1]);
                                }
                            };
                        case 3:
                            var d3 =
                                (Func<double, double, double, double>)
                                    Delegate.CreateDelegate(
                                        typeof(Func<double, double, double, double>),
                                        instance,
                                        method
                                    );
                            return (ref VMState state) =>
                            {
                                unsafe
                                {
                                    state.RegPtr[0] = d3(
                                        state.RegPtr[0],
                                        state.RegPtr[1],
                                        state.RegPtr[2]
                                    );
                                }
                            };
                        case 4:
                            var d4 =
                                (Func<double, double, double, double, double>)
                                    Delegate.CreateDelegate(
                                        typeof(Func<double, double, double, double, double>),
                                        instance,
                                        method
                                    );
                            return (ref VMState state) =>
                            {
                                unsafe
                                {
                                    state.RegPtr[0] = d4(
                                        state.RegPtr[0],
                                        state.RegPtr[1],
                                        state.RegPtr[2],
                                        state.RegPtr[3]
                                    );
                                }
                            };
                    }
                }
                else if (returnType == typeof(void))
                {
                    switch (parameters.Length)
                    {
                        case 0:
                            var a0 = (Action)
                                Delegate.CreateDelegate(typeof(Action), instance, method);
                            return (ref VMState state) => a0();
                        case 1:
                            var a1 =
                                (Action<double>)
                                    Delegate.CreateDelegate(
                                        typeof(Action<double>),
                                        instance,
                                        method
                                    );
                            return (ref VMState state) =>
                            {
                                unsafe
                                {
                                    a1(state.RegPtr[0]);
                                }
                            };
                        case 2:
                            var a2 =
                                (Action<double, double>)
                                    Delegate.CreateDelegate(
                                        typeof(Action<double, double>),
                                        instance,
                                        method
                                    );
                            return (ref VMState state) =>
                            {
                                unsafe
                                {
                                    a2(state.RegPtr[0], state.RegPtr[1]);
                                }
                            };
                        case 3:
                            var a3 =
                                (Action<double, double, double>)
                                    Delegate.CreateDelegate(
                                        typeof(Action<double, double, double>),
                                        instance,
                                        method
                                    );
                            return (ref VMState state) =>
                            {
                                unsafe
                                {
                                    a3(state.RegPtr[0], state.RegPtr[1], state.RegPtr[2]);
                                }
                            };
                        case 4:
                            var a4 =
                                (Action<double, double, double, double>)
                                    Delegate.CreateDelegate(
                                        typeof(Action<double, double, double, double>),
                                        instance,
                                        method
                                    );
                            return (ref VMState state) =>
                            {
                                unsafe
                                {
                                    a4(
                                        state.RegPtr[0],
                                        state.RegPtr[1],
                                        state.RegPtr[2],
                                        state.RegPtr[3]
                                    );
                                }
                            };
                    }
                }
            }

            // 2. Handle common single-parameter non-double signatures
            if (parameters.Length == 1)
            {
                var p0Type = parameters[0].ParameterType;
                if (returnType == typeof(double))
                {
                    if (p0Type == typeof(int))
                    {
                        var d =
                            (Func<int, double>)
                                Delegate.CreateDelegate(
                                    typeof(Func<int, double>),
                                    instance,
                                    method
                                );
                        return (ref VMState state) =>
                        {
                            unsafe
                            {
                                state.RegPtr[0] = d((int)state.RegPtr[0]);
                            }
                        };
                    }
                    if (p0Type == typeof(float))
                    {
                        var d =
                            (Func<float, double>)
                                Delegate.CreateDelegate(
                                    typeof(Func<float, double>),
                                    instance,
                                    method
                                );
                        return (ref VMState state) =>
                        {
                            unsafe
                            {
                                state.RegPtr[0] = d((float)state.RegPtr[0]);
                            }
                        };
                    }
                    if (p0Type == typeof(bool))
                    {
                        var d =
                            (Func<bool, double>)
                                Delegate.CreateDelegate(
                                    typeof(Func<bool, double>),
                                    instance,
                                    method
                                );
                        return (ref VMState state) =>
                        {
                            unsafe
                            {
                                state.RegPtr[0] = d(state.RegPtr[0] != 0.0);
                            }
                        };
                    }
                }
                else if (returnType == typeof(int))
                {
                    if (p0Type == typeof(int))
                    {
                        var d =
                            (Func<int, int>)
                                Delegate.CreateDelegate(typeof(Func<int, int>), instance, method);
                        return (ref VMState state) =>
                        {
                            unsafe
                            {
                                state.RegPtr[0] = (double)d((int)state.RegPtr[0]);
                            }
                        };
                    }
                }
                else if (returnType == typeof(bool))
                {
                    if (p0Type == typeof(double))
                    {
                        var d =
                            (Func<double, bool>)
                                Delegate.CreateDelegate(
                                    typeof(Func<double, bool>),
                                    instance,
                                    method
                                );
                        return (ref VMState state) =>
                        {
                            unsafe
                            {
                                state.RegPtr[0] = d(state.RegPtr[0]) ? 1.0 : 0.0;
                            }
                        };
                    }
                }
                else if (returnType == typeof(void))
                {
                    if (p0Type == typeof(int))
                    {
                        var a =
                            (Action<int>)
                                Delegate.CreateDelegate(typeof(Action<int>), instance, method);
                        return (ref VMState state) =>
                        {
                            unsafe
                            {
                                a((int)state.RegPtr[0]);
                            }
                        };
                    }
                    if (p0Type == typeof(float))
                    {
                        var a =
                            (Action<float>)
                                Delegate.CreateDelegate(typeof(Action<float>), instance, method);
                        return (ref VMState state) =>
                        {
                            unsafe
                            {
                                a((float)state.RegPtr[0]);
                            }
                        };
                    }
                    if (p0Type == typeof(bool))
                    {
                        var a =
                            (Action<bool>)
                                Delegate.CreateDelegate(typeof(Action<bool>), instance, method);
                        return (ref VMState state) =>
                        {
                            unsafe
                            {
                                a(state.RegPtr[0] != 0.0);
                            }
                        };
                    }
                }
            }
            // 3. Handle common two-parameter non-double signatures
            else if (parameters.Length == 2)
            {
                var p0Type = parameters[0].ParameterType;
                var p1Type = parameters[1].ParameterType;
                if (returnType == typeof(int))
                {
                    if (p0Type == typeof(int) && p1Type == typeof(int))
                    {
                        var d =
                            (Func<int, int, int>)
                                Delegate.CreateDelegate(
                                    typeof(Func<int, int, int>),
                                    instance,
                                    method
                                );
                        return (ref VMState state) =>
                        {
                            unsafe
                            {
                                state.RegPtr[0] = (double)d(
                                    (int)state.RegPtr[0],
                                    (int)state.RegPtr[1]
                                );
                            }
                        };
                    }
                }
            }

            return null;
        }

        // --------------------------------------------
        //  Type Conversion Helpers
        // --------------------------------------------

        private static readonly HashSet<Type> SupportedTypes = new()
        {
            typeof(double),
            typeof(float),
            typeof(int),
            typeof(uint),
            typeof(long),
            typeof(ulong),
            typeof(bool),
        };

        private static Func<double, object> CreateArgReader(Type type)
        {
            if (type == typeof(double))
                return reg => reg;
            if (type == typeof(float))
                return reg => (float)reg;
            if (type == typeof(int))
                return reg => (int)reg;
            if (type == typeof(uint))
                return reg => (uint)reg;
            if (type == typeof(long))
                return reg => (long)reg;
            if (type == typeof(ulong))
                return reg => (ulong)reg;
            if (type == typeof(bool))
                return reg => reg != 0.0;

            throw new NotSupportedException($"Unsupported parameter type: {type.FullName}");
        }

        private static Func<object, double> CreateReturnWriter(Type type)
        {
            if (type == typeof(double))
                return val => (double)val;
            if (type == typeof(float))
                return val => (double)(float)val;
            if (type == typeof(int))
                return val => (double)(int)val;
            if (type == typeof(uint))
                return val => (double)(uint)val;
            if (type == typeof(long))
                return val => (double)(long)val;
            if (type == typeof(ulong))
                return val => (double)(ulong)val;
            if (type == typeof(bool))
                return val => (bool)val ? 1.0 : 0.0;

            throw new NotSupportedException($"Unsupported return type: {type.FullName}");
        }

        // --------------------------------------------
        //  Validation
        // --------------------------------------------

        private void ValidateNoDuplicateName(string name, string source)
        {
            if (_methods.ContainsKey(name))
                throw new InvalidOperationException(
                    $"Duplicate FFI method name '{name}' (from {source}). A method with this name is already registered."
                );
        }

        private void ValidateNoDuplicateIndex(ushort index, string name)
        {
            if (_usedIndices.Contains(index))
                throw new InvalidOperationException(
                    $"Duplicate FFI method index {index} for method '{name}'. This index is already in use."
                );
        }

        private static void ValidateSupportedType(Type type, MethodInfo method, string paramName)
        {
            if (!SupportedTypes.Contains(type))
                throw new NotSupportedException(
                    $"Unsupported parameter type '{type.FullName}' on parameter '{paramName}' "
                        + $"of method '{method.DeclaringType?.Name}.{method.Name}'. "
                        + $"Supported types: double, float, int, uint, long, ulong, bool. "
                        + $"For full control, use the 'void (ref VMState state)' signature instead."
                );
        }

        private static void ValidateSupportedReturnType(Type type, MethodInfo method)
        {
            if (!SupportedTypes.Contains(type))
                throw new NotSupportedException(
                    $"Unsupported return type '{type.FullName}' on method '{method.DeclaringType?.Name}.{method.Name}'. "
                        + $"Supported types: void, double, float, int, uint, long, ulong, bool. "
                        + $"For full control, use the 'void (ref VMState state)' signature instead."
                );
        }

        // --------------------------------------------
        //  Utilities
        // --------------------------------------------

        private ushort AllocateNextIndex()
        {
            while (_usedIndices.Contains(_nextAutoIndex))
                _nextAutoIndex++;
            return _nextAutoIndex++;
        }

        /// <summary>
        /// Converts a PascalCase method name to camelCase.
        /// e.g., "SpawnEnemy" → "spawnEnemy", "X" → "x", "HTMLParser" → "htmlParser",
        /// "AB" → "ab", "ABC" → "abc".
        /// </summary>
        internal static string ToCamelCase(string name)
        {
            if (string.IsNullOrEmpty(name))
                return name;

            // Find the end of the leading uppercase run
            int upperRunLength = 0;
            for (int i = 0; i < name.Length && char.IsUpper(name[i]); i++)
                upperRunLength++;

            if (upperRunLength == 0)
                return name; // already camelCase or lowercase

            if (upperRunLength == 1)
                return char.ToLowerInvariant(name[0]) + name.Substring(1);

            // If entire string is uppercase, lowercase the whole thing
            if (upperRunLength == name.Length)
                return name.ToLowerInvariant();

            // For runs like "HTMLParser" → "htmlParser": lowercase all but the last uppercase letter
            return name.Substring(0, upperRunLength - 1).ToLowerInvariant()
                + name.Substring(upperRunLength - 1);
        }

        private static IReadOnlyDictionary<string, string>? CollectParameterDescriptions(
            MethodInfo method
        )
        {
            Dictionary<string, string>? descriptions = null;
            foreach (var attribute in method.GetCustomAttributes<RaptorParamAttribute>())
            {
                descriptions ??= new Dictionary<string, string>();
                if (attribute.ParamName == null)
                    throw new InvalidOperationException(
                        $"Parameter name for RaptorParamAttribute not found{(method.DeclaringType != null ? "in class " + method.DeclaringType.FullName : string.Empty)}"
                    );
                descriptions[attribute.ParamName] = attribute.Description;
            }
            foreach (var param in method.GetParameters())
            {
                var paramAttr = param.GetCustomAttribute<RaptorParamAttribute>();
                if (paramAttr != null)
                {
                    descriptions ??= new Dictionary<string, string>();
                    descriptions[param.Name!] = paramAttr.Description;
                }
            }
            return descriptions;
        }

        /// <summary>
        /// Generates TypeScript declaration file (.d.ts) content mapping all FFI modules
        /// and methods, complete with JSDoc comments for autocomplete documentation.
        /// </summary>
        public class MethodDefinition
        {
            public required string Type { get; set; }
            public required string Signature { get; set; }
            public string? Description { get; set; }
            public List<ParameterDefinition> Parameters { get; set; } = new();
        }

        public class ParameterDefinition
        {
            public required string Name { get; set; }
            public string? Description { get; set; }
        }

        public string GenerateAutocompleteDeclarations()
        {
            var sb = new StringBuilder();
            // Group methods by namespace
            // e.g. "math.clamp" -> namespace "math", method "clamp"
            // "spawnEnemy"      -> namespace "global", method "spawnEnemy"
            var methods = new Dictionary<string, MethodDefinition>();

            foreach (var info in _methodInfos)
            {
                var methodParams = new List<ParameterDefinition>();
                foreach (var paramName in info.ParameterNames)
                {
                    methodParams.Add(
                        new ParameterDefinition
                        {
                            Name = paramName,
                            Description = info.ParameterDescriptions?[paramName],
                        }
                    );
                }
                methods.Add(
                    info.Name,
                    new MethodDefinition
                    {
                        Type = "method",
                        Signature = $"{info.Name}({string.Join(", ", info.ParameterNames)})",
                        Description = info.Description,
                        Parameters = methodParams,
                    }
                );
            }

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true,
            };
            sb.Append(
                JsonSerializer.Serialize(
                    methods,
                    AppJsonSerializerContext.Default.DictionaryStringMethodDefinition
                )
            );
            return sb.ToString();
        }
    }

    [JsonSourceGenerationOptions(
        PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
        WriteIndented = true
    )]
    [JsonSerializable(typeof(Dictionary<string, FFIHostTable.MethodDefinition>))]
    internal partial class AppJsonSerializerContext : JsonSerializerContext { }
}
