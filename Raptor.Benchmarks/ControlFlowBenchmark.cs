using System;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Attributes;
using Raptor;

namespace Raptor.Benchmarks;

[MemoryDiagnoser]
public class ControlFlowBenchmark
{
    private VirtualMachine _vm = null!;
    private VMChunk _predictableChunk = null!;
    private VMChunk _unpredictableChunk = null!;

    private const int Epochs = 30000;

    [GlobalSetup]
    public void Setup()
    {
        _vm = new VirtualMachine();
        var table = new FFIHostTable();
        
        // 1. Predictable FFI populates array with all 0.1s
        table.Register("populateDataPredictable", 10, (ref VMState state) => {
            unsafe {
                double* arrPtr = (double*)(ulong)state.RegPtr[0];
                int size = Epochs;
                for (int i = 0; i < size; i++) {
                    arrPtr[i] = 0.1;
                }
            }
        });

        // 2. Unpredictable FFI populates array with random doubles (0.0 to 1.0)
        var random = new Random(42);
        table.Register("populateDataUnpredictable", 11, (ref VMState state) => {
            unsafe {
                double* arrPtr = (double*)(ulong)state.RegPtr[0];
                int size = Epochs;
                for (int i = 0; i < size; i++) {
                    arrPtr[i] = random.NextDouble();
                }
            }
        });

        _vm.RegisterHostTable(table);

        var engine = new ScriptEngine();
        engine.RegisterHostTable(table);

        _predictableChunk = engine.Compile($@"
            DEFINE epochs {Epochs}
            DEFINE i r5
            DEFINE val r1
            DEFINE arr r2
            DEFINE threshold 0.5
            NEWARR arr epochs
            CALL populateDataPredictable() arr
            LOADC i 0
            loop:
                GETARR val arr i
                LT 1 val threshold
                JUMP branch_taken
            branch_taken:
                FOR i epochs 1 < loop
            FREEARR arr
            HALT");

        _unpredictableChunk = engine.Compile($@"
            DEFINE epochs {Epochs}
            DEFINE i r5
            DEFINE val r1
            DEFINE arr r2
            DEFINE threshold 0.5
            NEWARR arr epochs
            CALL populateDataUnpredictable() arr
            LOADC i 0
            loop:
                GETARR val arr i
                LT 1 val threshold
                JUMP branch_taken
            branch_taken:
                FOR i epochs 1 < loop
            FREEARR arr
            HALT");
    }

    [Benchmark(Baseline = true)]
    public void Benchmark_PredictableBranch()
    {
        _vm.LoadProgram(_predictableChunk);
        _vm.RunFast();
    }

    [Benchmark]
    public void Benchmark_UnpredictableBranch()
    {
        _vm.LoadProgram(_unpredictableChunk);
        _vm.RunFast();
    }
}
