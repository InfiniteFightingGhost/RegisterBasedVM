using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Raptor.Compiler;

namespace Raptor
{
    /// <summary>
    /// High-level wrapper that hides VMChunk creation, assembler passes, bytecode verification,
    /// pointer pinning, and stack allocations behind simple, safe methods.
    ///
    /// <examples>
    ///   <code>
    ///   var engine = new ScriptEngine();
    ///   engine.RegisterHostMethod("spawnEnemy", 0, myHandler);
    ///   var result = engine.Execute("script.rbc");
    ///   </code>
    /// </examples>
    /// </summary>
    public sealed class ScriptEngine : IDisposable
    {
        private readonly VirtualMachine _vm;
        private readonly Dictionary<
            string,
            (ushort index, VirtualMachine.HostFFIDelegate callback)
        > _hostMethods;
        private readonly int _heapSizeBytes;

        /// <summary>
        /// Creates a new ScriptEngine with the specified heap budget.
        /// </summary>
        /// <param name="heapSizeBytes">Heap size in bytes for bytecode verification bounds checks. Default: 512 KB.</param>
        public ScriptEngine(int heapSizeBytes = 512 * 1024)
        {
            _vm = new VirtualMachine();
            _hostMethods = new();
            _heapSizeBytes = heapSizeBytes;
        }

        /// <summary>
        /// Registers all host FFI methods from an <see cref="FFIHostTable"/>.
        /// Must be called before Compile() or Run() for the methods to be available.
        /// </summary>
        public void RegisterHostTable(FFIHostTable table)
        {
            foreach (var (name, (index, callback)) in table.Methods)
            {
                RegisterHostMethod(name, index, callback);
            }
            foreach (var pair in table.UnmanagedCallbacks)
            {
                unsafe
                {
                    _vm.RegisterHostMethod(
                        pair.Key,
                        (delegate* managed<ref VMState, void>)pair.Value
                    );
                }
            }
        }

        /// <summary>
        /// Registers a host FFI method that scripts can call by name.
        /// Must be called before Compile() or Run() for the method to be available.
        /// </summary>
        public void RegisterHostMethod(
            string name,
            ushort index,
            VirtualMachine.HostFFIDelegate callback
        )
        {
            _hostMethods.Add(name, (index, callback));
            _vm.RegisterHostMethod(index, callback);
        }

        /// <summary>
        /// Registers an unmanaged host FFI method function pointer that scripts can call by name.
        /// Must be called before Compile() or Run() for the method to be available.
        /// </summary>
        public unsafe void RegisterHostMethod(
            string name,
            ushort index,
            delegate* managed<ref VMState, void> callback
        )
        {
            VirtualMachine.HostFFIDelegate managedWrapper = (ref VMState state) =>
                callback(ref state);
            _hostMethods.Add(name, (index, managedWrapper));
            _vm.RegisterHostMethod(index, callback);
        }

        /// <summary>
        /// Compiles high-level RaptorScript source code (.rapt) into a verified VMChunk.
        /// </summary>
        public VMChunk CompileRaptorScript(string raptorScriptSource)
        {
            var reporter = new DiagnosticReporter();
            string asm = RaptorScriptCompiler.Compile(raptorScriptSource, reporter: reporter);
            if (reporter.HasErrors)
            {
                var firstErr = reporter.Diagnostics.FirstOrDefault();
                string errMsg = firstErr != null ? firstErr.Message : "Syntax errors detected.";
                throw new CompileException(errMsg);
            }
            return CompileAssembly(raptorScriptSource: asm);
        }

        /// <summary>
        /// Compiles high-level RaptorScript source code (.rapt) and executes it in one call.
        /// </summary>
        public ExecutionResult RunRaptorScript(string raptorScriptSource)
        {
            var chunk = CompileRaptorScript(raptorScriptSource);
            return Execute(chunk);
        }

        /// <summary>
        /// Compiles source text (RaptorScript or Raptor Assembly) into a verified VMChunk.
        /// </summary>
        public VMChunk Compile(string sourceText)
        {
            if (IsRaptorScript(sourceText))
            {
                return CompileRaptorScript(sourceText);
            }
            return CompileAssembly(sourceText);
        }

        private VMChunk CompileAssembly(string raptorScriptSource)
        {
            var chunk = new VMChunk();
            var assembler = new Assembler(chunk);

            foreach (var (name, (index, _)) in _hostMethods)
            {
                assembler.RegisterHostMethod(name, index);
            }

            assembler.Parse(raptorScriptSource.Split('\n').ToList());
            BytecodeVerifier.Verify(chunk, _heapSizeBytes);
            return chunk;
        }

        private static bool IsRaptorScript(string source)
        {
            if (string.IsNullOrWhiteSpace(source))
                return false;

            string[] lines = source.Split('\n');
            foreach (var rawLine in lines)
            {
                string line = rawLine.Trim();
                if (string.IsNullOrEmpty(line) || line.StartsWith(";") || line.StartsWith("//"))
                    continue;

                if (line.StartsWith("var ") || line.StartsWith("for") || line.StartsWith("while") ||
                    line.StartsWith("if") || line.StartsWith("return") || line.Contains(";"))
                {
                    return true;
                }

                string token = line.Split(' ', '\t')[0].ToUpperInvariant();
                if (Enum.TryParse<OpCode>(token, ignoreCase: true, out _))
                {
                    return false;
                }
                break;
            }
            return false;
        }

        /// <summary>
        /// Disposes underlying VirtualMachine resources and frees pinned GCHandles.
        /// </summary>
        public void Dispose()
        {
            _vm.Dispose();
        }

        /// <summary>
        /// Compiles a .rasm assembly source file into a verified VMChunk.
        /// </summary>
        public VMChunk CompileFile(string filePath)
        {
            string sourceText = System.IO.File.ReadAllText(filePath);
            return Compile(sourceText);
        }

        public ExecutionResult Execute(VMChunk chunk)
        {
            _vm.LoadProgram(chunk);
            return _vm.RunFast();
        }

        /// <summary>
        /// Executes a pre-compiled VMChunk in profiling mode.
        /// </summary>
        public ExecutionResult ExecuteProfile(
            VMChunk chunk,
            ulong[] opcodeCounters,
            out ulong totalInstructions
        )
        {
            _vm.LoadProgram(chunk);
            return _vm.RunProfile(opcodeCounters, out totalInstructions);
        }

        /// <summary>
        /// Loads a compiled .rbc binary file and executes it.
        /// </summary>
        public ExecutionResult Execute(string filePath)
        {
            var chunk = RaptorBinary.Load(filePath);
            return Execute(chunk);
        }

        /// <summary>
        /// Compiles assembly source text, verifies it, and executes it in one call.
        /// </summary>
        public ExecutionResult Run(string sourceText)
        {
            var chunk = Compile(sourceText);
            return Execute(chunk);
        }

        /// <summary>
        /// Compiles a .rasm assembly source file, verifies it, and executes it in one call.
        /// </summary>
        public ExecutionResult RunFile(string filePath)
        {
            var chunk = CompileFile(filePath);
            return Execute(chunk);
        }

        /// <summary>
        /// Executes a pre-compiled VMChunk with per-instruction debug hook callbacks.
        /// </summary>
        public ExecutionResult ExecuteDebug(VMChunk chunk, VirtualMachine.DebugHook hook)
        {
            _vm.LoadProgram(chunk);
            return _vm.RunDebug(hook);
        }

        /// <summary>
        /// Saves a compiled VMChunk to a .rbc binary file.
        /// </summary>
        public void SaveToFile(VMChunk chunk, string filePath)
        {
            RaptorBinary.Save(chunk, filePath);
        }

        /// <summary>
        /// Loads a compiled VMChunk from a .rbc binary file.
        /// </summary>
        public VMChunk LoadFromFile(string filePath)
        {
            return RaptorBinary.Load(filePath);
        }

        /// <summary>
        /// Disassembles a compiled VMChunk into human-readable assembly text.
        /// </summary>
        public string Disassemble(VMChunk chunk)
        {
            return Disassembler.Disassemble(chunk);
        }

        /// <summary>
        /// Disassembles a .rbc binary file and disassembles it into human-readable assembly text.
        /// </summary>
        public string Disassemble(string filePath)
        {
            var chunk = RaptorBinary.Load(filePath);
            return Disassembler.Disassemble(chunk);
        }

        /// <summary>
        /// Translates a runtime error's program counter instruction pointer (IP)
        /// to the original RaptorScript source line if a SourceMap is available.
        /// </summary>
        public static string TranslateError(VMChunk chunk, int ip, string raptorScriptSource)
        {
            if (chunk.SourceMap == null)
                return $"Runtime error at instruction {ip} (no source map available).";

            int line = chunk.SourceMap.GetLineNumber(ip);
            if (line <= 0)
                return $"Runtime error at instruction {ip} (unmapped line).";

            string[] sourceLines = raptorScriptSource.Split(
                new[] { "\r\n", "\r", "\n" },
                System.StringSplitOptions.None
            );
            if (line - 1 >= 0 && line - 1 < sourceLines.Length)
            {
                string codeSnippet = sourceLines[line - 1].Trim();
                return $"Runtime error at line {line}: \"{codeSnippet}\"";
            }

            return $"Runtime error at line {line}";
        }
    }
}
