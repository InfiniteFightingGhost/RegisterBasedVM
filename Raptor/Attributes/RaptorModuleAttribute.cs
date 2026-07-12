using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Raptor.Attributes
{
/// <summary>
/// Marks a class or struct as a Raptor FFI module.
/// Only types decorated with this attribute are scanned during assembly-wide registration
/// via <see cref="Raptor.FFIHostTable.FromAssembly"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public sealed class RaptorModuleAttribute : Attribute
{
    /// <summary>
    /// Optional prefix for method names. When set, all methods in this module
    /// are registered as "prefix.methodName" (e.g., "math.add").
    /// </summary>
    public string? Prefix { get; }

    /// <summary>
    /// Registers a module with no prefix.
    /// </summary>
    public RaptorModuleAttribute()
    {
        Prefix = null;
    }

    /// <summary>
    /// Registers a module with a method name prefix.
    /// </summary>
    /// <param name="prefix">Prefix prepended to all method names (e.g., "math" → "math.add").</param>
    public RaptorModuleAttribute(string prefix)
    {
        Prefix = prefix;
    }
}
}
