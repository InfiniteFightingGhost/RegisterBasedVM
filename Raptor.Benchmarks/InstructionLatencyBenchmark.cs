using System;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Attributes;
using Raptor;

namespace Raptor.Benchmarks;

[MemoryDiagnoser]
public class InstructionLatencyBenchmark
{
    private VirtualMachine _vm = null!;
    private VMChunk _baseline = null!;
    private VMChunk _add = null!;
    private VMChunk _sub = null!;
    private VMChunk _mul = null!;
    private VMChunk _div = null!;
    private VMChunk _sqrt = null!;
    private VMChunk _fisr = null!;
    private VMChunk _rand = null!;
    private VMChunk _loadc = null!;
    private VMChunk _move = null!;
    private VMChunk _jump = null!;

    private const int Epochs = 50000;

    [GlobalSetup]
    public void Setup()
    {
        // Redirect stdout/stderr to avoid benchmark pollution


        _vm = new VirtualMachine();
        var engine = new ScriptEngine();

        _baseline = engine.Compile($@"
            DEFINE epochs {Epochs}
            DEFINE i r5
            LOADC i 0
            loop:
                FOR i epochs 1 < loop
            HALT");

        _add = engine.Compile($@"
            DEFINE epochs {Epochs}
            DEFINE i r5
            LOADC r1 1.5
            LOADC r2 2.5
            LOADC i 0
            loop:
                ADD r3 r1 r2
                FOR i epochs 1 < loop
            HALT");

        _sub = engine.Compile($@"
            DEFINE epochs {Epochs}
            DEFINE i r5
            LOADC r1 1.5
            LOADC r2 2.5
            LOADC i 0
            loop:
                SUB r3 r1 r2
                FOR i epochs 1 < loop
            HALT");

        _mul = engine.Compile($@"
            DEFINE epochs {Epochs}
            DEFINE i r5
            LOADC r1 1.5
            LOADC r2 2.5
            LOADC i 0
            loop:
                MUL r3 r1 r2
                FOR i epochs 1 < loop
            HALT");

        _div = engine.Compile($@"
            DEFINE epochs {Epochs}
            DEFINE i r5
            LOADC r1 10.0
            LOADC r2 2.0
            LOADC i 0
            loop:
                DIV r3 r1 r2
                FOR i epochs 1 < loop
            HALT");

        _sqrt = engine.Compile($@"
            DEFINE epochs {Epochs}
            DEFINE i r5
            LOADC r1 16.0
            LOADC i 0
            loop:
                SQRT r3 r1
                FOR i epochs 1 < loop
            HALT");

        _fisr = engine.Compile($@"
            DEFINE epochs {Epochs}
            DEFINE i r5
            LOADC r1 16.0
            LOADC i 0
            loop:
                FISR r3 r1
                FOR i epochs 1 < loop
            HALT");

        _rand = engine.Compile($@"
            DEFINE epochs {Epochs}
            DEFINE i r5
            LOADC i 0
            loop:
                RAND r1
                FOR i epochs 1 < loop
            HALT");

        _loadc = engine.Compile($@"
            DEFINE epochs {Epochs}
            DEFINE i r5
            LOADC i 0
            loop:
                LOADC r1 5.5
                FOR i epochs 1 < loop
            HALT");

        _move = engine.Compile($@"
            DEFINE epochs {Epochs}
            DEFINE i r5
            LOADC r2 7.7
            LOADC i 0
            loop:
                MOVE r1 r2
                FOR i epochs 1 < loop
            HALT");

        _jump = engine.Compile($@"
            DEFINE epochs {Epochs}
            DEFINE i r5
            LOADC i 0
            loop:
                JUMP target
            target:
                FOR i epochs 1 < loop
            HALT");

        // Warm up and pre-load baseline program so registers array pins correctly
        _vm.LoadProgram(_baseline);
    }

    [Benchmark(Baseline = true)]
    public void Benchmark_Baseline()
    {
        _vm.LoadProgram(_baseline);
        _vm.RunFast();
    }

    [Benchmark]
    public void Benchmark_Add()
    {
        _vm.LoadProgram(_add);
        _vm.RunFast();
    }

    [Benchmark]
    public void Benchmark_Sub()
    {
        _vm.LoadProgram(_sub);
        _vm.RunFast();
    }

    [Benchmark]
    public void Benchmark_Mul()
    {
        _vm.LoadProgram(_mul);
        _vm.RunFast();
    }

    [Benchmark]
    public void Benchmark_Div()
    {
        _vm.LoadProgram(_div);
        _vm.RunFast();
    }

    [Benchmark]
    public void Benchmark_Sqrt()
    {
        _vm.LoadProgram(_sqrt);
        _vm.RunFast();
    }

    [Benchmark]
    public void Benchmark_Fisr()
    {
        _vm.LoadProgram(_fisr);
        _vm.RunFast();
    }

    [Benchmark]
    public void Benchmark_Rand()
    {
        _vm.LoadProgram(_rand);
        _vm.RunFast();
    }

    [Benchmark]
    public void Benchmark_Loadc()
    {
        _vm.LoadProgram(_loadc);
        _vm.RunFast();
    }

    [Benchmark]
    public void Benchmark_Move()
    {
        _vm.LoadProgram(_move);
        _vm.RunFast();
    }

    [Benchmark]
    public void Benchmark_Jump()
    {
        _vm.LoadProgram(_jump);
        _vm.RunFast();
    }
}
