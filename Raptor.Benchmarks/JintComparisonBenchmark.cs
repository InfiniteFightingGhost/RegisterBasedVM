using System;
using BenchmarkDotNet.Attributes;
using Jint;
using Jint.Native;
using Raptor;

namespace Raptor.Benchmarks;

[MemoryDiagnoser]
public class JintComparisonBenchmark
{
    private VirtualMachine _raptorVm = null!;
    private ScriptEngine _raptorEngine = null!;
    private Engine _jintFFI = null!;
    private Engine _jintFib = null!;
    private Engine _jintArray = null!;
    private Engine _jintEcs = null!;

    private VMChunk _raptorFFI = null!;
    private VMChunk _raptorFib = null!;
    private VMChunk _raptorArray = null!;
    private VMChunk _raptorEcs = null!;

    private const int FfiIterations = 10000;
    private const int FibN = 30;
    private const int ArraySize = 100;
    private const int EcsCount = 1000;

    public static double HostAdd(double a, double b) => a + b;

    [GlobalSetup]
    public void Setup()
    {
        // 1. Setup Raptor VM
        _raptorVm = new VirtualMachine();
        _raptorEngine = new ScriptEngine();

        var ffiTable = new FFIHostTable();
        ffiTable.Register("hostAdd", 1, (ref VMState state) =>
        {
            unsafe
            {
                state.RegPtr[0] = state.RegPtr[0] + 1.0;
            }
        });
        _raptorEngine.RegisterHostTable(ffiTable);

        _raptorFFI = _raptorEngine.Compile($@"
            DEFINE count {FfiIterations}
            DEFINE i r0
            LOADC i 0
            loop:
                CALL hostAdd() r0
                FOR i count 1 < loop
            HALT");

        _raptorFib = _raptorEngine.Compile($@"
            DEFINE n {FibN}
            DEFINE a r0
            DEFINE b r1
            DEFINE i r2
            DEFINE temp r3
            LOADC a 0.0
            LOADC b 1.0
            LOADC i 1.0
            loop:
                ADD temp a b
                MOVE a b
                MOVE b temp
                FOR i n 1 < loop
            HALT");

        _raptorArray = _raptorEngine.Compile($@"
            NEWARR r0 {ArraySize}
            LOADC r1 0.0
            LOADC r2 1.5
            loop_init:
                MUL r3 r1 r2
                SETARR r0 r1 r3
                FOR r1 {ArraySize} 1 < loop_init
            LOADC r1 0.0
            LOADC r4 0.0
            loop_sum:
                GETARR r3 r0 r1
                ADD r4 r4 r3
                FOR r1 {ArraySize} 1 < loop_sum
            FREEARR r0
            HALT");

        _raptorEcs = _raptorEngine.Compile($@"
            NEWARR r0 {EcsCount}
            NEWARR r1 {EcsCount}
            NEWARR r2 {EcsCount}
            NEWARR r3 {EcsCount}
            LOADC r4 0.0
            LOADC r5 1.0
            init_loop:
                SETARR r0 r4 r5
                SETARR r1 r4 r5
                SETARR r2 r4 r5
                SETARR r3 r4 r5
                FOR r4 {EcsCount} 1 < init_loop
            LOADC r4 0.0
            LOADC r6 0.016
            update_loop:
                GETARR r7 r2 r4
                GETARR r8 r0 r4
                MUL r9 r7 r6
                ADD r8 r8 r9
                SETARR r0 r4 r8
                GETARR r7 r3 r4
                GETARR r8 r1 r4
                MUL r9 r7 r6
                ADD r8 r8 r9
                SETARR r1 r4 r8
                FOR r4 {EcsCount} 1 < update_loop
            FREEARR r0
            FREEARR r1
            FREEARR r2
            FREEARR r3
            HALT");

        // 2. Setup Jint Engines
        _jintFFI = new Engine();
        _jintFFI.SetValue("hostAdd", (Func<double, double, double>)HostAdd);
        _jintFFI.Evaluate($@"
            function runFFI() {{
                let count = {FfiIterations};
                let res = 0;
                for (let i = 0; i < count; i++) {{
                    res = hostAdd(i, 1.0);
                }}
                return res;
            }}");

        _jintFib = new Engine();
        _jintFib.Evaluate($@"
            function runFib() {{
                let n = {FibN};
                let a = 0.0, b = 1.0;
                for (let i = 0; i < n; i++) {{
                    let temp = a + b;
                    a = b;
                    b = temp;
                }}
                return b;
            }}");

        _jintArray = new Engine();
        _jintArray.Evaluate($@"
            function runArray() {{
                let size = {ArraySize};
                let t = new Array(size);
                for (let i = 0; i < size; i++) {{
                    t[i] = i * 1.5;
                }}
                let sum = 0;
                for (let i = 0; i < size; i++) {{
                    sum += t[i];
                }}
                return sum;
            }}");

        _jintEcs = new Engine();
        _jintEcs.Evaluate($@"
            function runECS() {{
                let count = {EcsCount};
                let px = new Float64Array(count);
                let py = new Float64Array(count);
                let vx = new Float64Array(count);
                let vy = new Float64Array(count);
                for (let i = 0; i < count; i++) {{
                    px[i] = 1.0;
                    py[i] = 1.0;
                    vx[i] = 1.0;
                    vy[i] = 1.0;
                }}
                let dt = 0.016;
                for (let i = 0; i < count; i++) {{
                    px[i] = px[i] + vx[i] * dt;
                    py[i] = py[i] + vy[i] * dt;
                }}
            }}");
    }

    // -------------------------------------------------------------
    // FFI Overhead Benchmark
    // -------------------------------------------------------------
    [Benchmark(Baseline = true)]
    public ExecutionResult Raptor_FFI_Overhead() => _raptorEngine.Execute(_raptorFFI);

    [Benchmark]
    public JsValue Jint_FFI_Overhead() => _jintFFI.Invoke("runFFI");

    // -------------------------------------------------------------
    // Fibonacci Benchmark
    // -------------------------------------------------------------
    [Benchmark]
    public ExecutionResult Raptor_Fibonacci() => _raptorEngine.Execute(_raptorFib);

    [Benchmark]
    public JsValue Jint_Fibonacci() => _jintFib.Invoke("runFib");

    // -------------------------------------------------------------
    // Array Access Benchmark
    // -------------------------------------------------------------
    [Benchmark]
    public ExecutionResult Raptor_ArrayAccess() => _raptorEngine.Execute(_raptorArray);

    [Benchmark]
    public JsValue Jint_ArrayAccess() => _jintArray.Invoke("runArray");

    // -------------------------------------------------------------
    // ECS Update Benchmark
    // -------------------------------------------------------------
    [Benchmark]
    public ExecutionResult Raptor_EcsUpdate() => _raptorEngine.Execute(_raptorEcs);

    [Benchmark]
    public JsValue Jint_EcsUpdate() => _jintEcs.Invoke("runECS");
}
