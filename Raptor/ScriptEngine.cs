namespace Raptor;

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
public sealed class ScriptEngine
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
    /// <param name="heapSizeBytes">Heap size in bytes for bytecode verification bounds checks. Default: 16 MB.</param>
    public ScriptEngine(int heapSizeBytes = 16 * 1024 * 1024)
    {
        _vm = new VirtualMachine();
        _hostMethods = new();
        _heapSizeBytes = heapSizeBytes;
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
        _hostMethods[name] = (index, callback);
        _vm.RegisterHostMethod(index, callback);
    }

    /// <summary>
    /// Compiles assembly source text into a verified VMChunk.
    /// </summary>
    public VMChunk Compile(string sourceText)
    {
        var chunk = new VMChunk();
        var assembler = new Assembler(chunk);

        // Register any host methods so the assembler can resolve CALL instructions
        foreach (var (name, (index, _)) in _hostMethods)
        {
            assembler.RegisterHostMethod(name, index);
        }

        assembler.Parse(sourceText.Split('\n').ToList());
        BytecodeVerifier.Verify(chunk, _heapSizeBytes);
        return chunk;
    }

    /// <summary>
    /// Executes a pre-compiled VMChunk.
    /// </summary>
    public ExecutionResult Execute(VMChunk chunk)
    {
        _vm.LoadProgram(chunk);
        return _vm.RunFast();
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
        return VirtualMachine.Disassemble(chunk);
    }

    /// <summary>
    /// Loads a .rbc binary file and disassembles it into human-readable assembly text.
    /// </summary>
    public string Disassemble(string filePath)
    {
        var chunk = RaptorBinary.Load(filePath);
        return VirtualMachine.Disassemble(chunk);
    }
}
