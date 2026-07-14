using System;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Attributes;
using Raptor;

namespace Raptor.Benchmarks;

[MemoryDiagnoser]
public class LifecycleBenchmark
{
    private VirtualMachine _vm = null!;
    private ScriptEngine _engine = null!;
    private VMChunk _physicsChunk = null!;
    private string _asmSource = null!;

    [GlobalSetup]
    public void Setup()
    {


        _vm = new VirtualMachine();
        _engine = new ScriptEngine();

        _asmSource = @"
            DEFINE epochs 10000
            DEFINE i r8
            LOADC r1 0.0
            LOADC r2 10.0
            LOADC r3 2.5
            LOADC r4 0.0
            LOADC r5 9.81
            LOADC r6 0.016
            LOADC r7 0.0
            LOADC i 0
            loop:
                MUL r9 r5 r6
                SUB r4 r4 r9
                MUL r10 r3 r6
                ADD r1 r1 r10
                MUL r11 r4 r6
                ADD r2 r2 r11
                LT 0 r2 r7
                JUMP skip_ground
                MOVE r2 r7
                LOADC r4 0.0
            skip_ground:
                FOR i epochs 1 < loop
            HALT";

        _physicsChunk = _engine.Compile(_asmSource);
        _vm.LoadProgram(_physicsChunk);
    }

    [Benchmark]
    public void Lifecycle_Compile()
    {
        // Parse and compile source assembly text into a chunk
        _engine.Compile(_asmSource);
    }

    [Benchmark]
    public void Lifecycle_Verify()
    {
        // Verify bytecode correctness
        BytecodeVerifier.Verify(_physicsChunk, 16 * 1024 * 1024);
    }

    [Benchmark]
    public void Lifecycle_Load()
    {
        // Pin array handles and load program state
        _vm.LoadProgram(_physicsChunk);
    }

    [Benchmark]
    public void Lifecycle_Execute()
    {
        // Execute the isolated pre-loaded bytecode program
        _vm.RunFast();
    }
}
