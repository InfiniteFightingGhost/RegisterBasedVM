using System;
using BenchmarkDotNet.Attributes;
using MoonSharp.Interpreter;
using Raptor;

namespace Raptor.Benchmarks;

[MemoryDiagnoser]
public class MoonSharpComparisonBenchmark
{
    private VirtualMachine _raptorVm = null!;
    private ScriptEngine _raptorEngine = null!;
    private Script _moonScriptFFI = null!;
    private Script _moonScriptFib = null!;
    private Script _moonScriptArray = null!;
    private Script _moonScriptEcs = null!;

    private VMChunk _raptorFFI = null!;
    private VMChunk _raptorFib = null!;
    private VMChunk _raptorArray = null!;
    private VMChunk _raptorEcs = null!;

    private const int FfiIterations = 10000;
    private const int FibN = 30;
    private const int ArraySize = 100;
    private const int EcsCount = 1000;

    // Static host method for MoonSharp FFI test
    public static double HostAdd(double a, double b) => a + b;

    [GlobalSetup]
    public void Setup()
    {
        // 1. Setup Raptor VM
        _raptorVm = new VirtualMachine();
        _raptorEngine = new ScriptEngine();

        // Register FFI method on Raptor using high-performance VMState callback
        var ffiTable = new FFIHostTable();
        ffiTable.Register("hostAdd", 1, (ref VMState state) =>
        {
            unsafe
            {
                // RegPtr[0] is arg i, increment in place
                state.RegPtr[0] = state.RegPtr[0] + 1.0;
            }
        });
        _raptorEngine.RegisterHostTable(ffiTable);

        // Compile Raptor scripts
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

        // 2. Setup MoonSharp Script Engine
        UserData.RegisterType<MoonSharpComparisonBenchmark>();

        // MoonSharp FFI
        _moonScriptFFI = new Script();
        _moonScriptFFI.Globals["hostAdd"] = (Func<double, double, double>)HostAdd;
        _moonScriptFFI.DoString($@"
            function runFFI()
                local count = {FfiIterations}
                local res = 0
                for i = 1, count do
                    res = hostAdd(i, 1.0)
                end
                return res
            end");

        // MoonSharp Fibonacci
        _moonScriptFib = new Script();
        _moonScriptFib.DoString($@"
            function runFib()
                local n = {FibN}
                local a, b = 0.0, 1.0
                for i = 1, n do
                    local temp = a + b
                    a = b
                    b = temp
                end
                return b
            end");

        // MoonSharp Array
        _moonScriptArray = new Script();
        _moonScriptArray.DoString($@"
            function runArray()
                local size = {ArraySize}
                local t = {{}}
                for i = 1, size do
                    t[i] = i * 1.5
                end
                local sum = 0
                for i = 1, size do
                    sum = sum + t[i]
                end
                return sum
            end");

        // MoonSharp ECS
        _moonScriptEcs = new Script();
        _moonScriptEcs.DoString($@"
            function runECS()
                local count = {EcsCount}
                local px, py, vx, vy = {{}}, {{}}, {{}}, {{}}
                for i = 1, count do
                    px[i] = 1.0
                    py[i] = 1.0
                    vx[i] = 1.0
                    vy[i] = 1.0
                end
                local dt = 0.016
                for i = 1, count do
                    px[i] = px[i] + vx[i] * dt
                    py[i] = py[i] + vy[i] * dt
                end
            end");
    }

    // -------------------------------------------------------------
    // FFI Overhead Benchmark
    // -------------------------------------------------------------
    [Benchmark(Baseline = true)]
    public ExecutionResult Raptor_FFI_Overhead()
    {
        return _raptorEngine.Execute(_raptorFFI);
    }

    [Benchmark]
    public DynValue MoonSharp_FFI_Overhead()
    {
        return _moonScriptFFI.Call(_moonScriptFFI.Globals["runFFI"]);
    }

    // -------------------------------------------------------------
    // Fibonacci Benchmark
    // -------------------------------------------------------------
    [Benchmark]
    public ExecutionResult Raptor_Fibonacci()
    {
        return _raptorEngine.Execute(_raptorFib);
    }

    [Benchmark]
    public DynValue MoonSharp_Fibonacci()
    {
        return _moonScriptFib.Call(_moonScriptFib.Globals["runFib"]);
    }

    // -------------------------------------------------------------
    // Array Access Benchmark
    // -------------------------------------------------------------
    [Benchmark]
    public ExecutionResult Raptor_ArrayAccess()
    {
        return _raptorEngine.Execute(_raptorArray);
    }

    [Benchmark]
    public DynValue MoonSharp_ArrayAccess()
    {
        return _moonScriptArray.Call(_moonScriptArray.Globals["runArray"]);
    }

    // -------------------------------------------------------------
    // ECS Update Benchmark
    // -------------------------------------------------------------
    [Benchmark]
    public ExecutionResult Raptor_EcsUpdate()
    {
        return _raptorEngine.Execute(_raptorEcs);
    }

    [Benchmark]
    public DynValue MoonSharp_EcsUpdate()
    {
        return _moonScriptEcs.Call(_moonScriptEcs.Globals["runECS"]);
    }
}
