using System;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Attributes;
using Raptor;
using Raptor.Attributes;

namespace Raptor.Benchmarks;

[MemoryDiagnoser]
public class CallStackBenchmark
{
    private VirtualMachine _vm = null!;
    private VMChunk _depth10Chunk = null!;
    private VMChunk _depth30Chunk = null!;
    private VMChunk _internalCallChunk = null!;
    private VMChunk _ffiDirectChunk = null!;
    private VMChunk _ffiTypedChunk = null!;
    private VMChunk _ffiFallbackChunk = null!;
    private VMChunk _ffiDirectOverhead = null!;

    [GlobalSetup]
    public void Setup()
    {
        _vm = new VirtualMachine();
        var table = new FFIHostTable();

        table.Register(
            "directAdd",
            100,
            (ref VMState state) =>
            {
                unsafe
                {
                    state.RegPtr[0] = state.RegPtr[0] + state.RegPtr[0];
                }
            }
        );

        // Register typed and fallback modules
        table.RegisterModule(typeof(FfiBenchmarkBindings));
        table.RegisterModule(typeof(FallbackBenchmarkBindings));
        table.Register("lazy", 115, (ref VMState state) => { });

        _vm.RegisterHostTable(table);

        var engine = new ScriptEngine();
        engine.RegisterHostTable(table);

        // Recursive call depth = 10
        _depth10Chunk = engine.Compile(
            @"
            DEFINE epochs 10000
            DEFINE i r5
            DEFINE depth r1
            LOADC i 0
            loop:
                LOADC depth 10
                CALL recurse() depth
                FOR i epochs 1 < loop
            HALT

            recurse()
                EQ 0 r0 0
                JUMP base_case
                SUB r0 r0 1
                CALL recurse() r0
            base_case:
                RETURN r0 r0"
        );

        // Recursive call depth = 30 (close to hard limit of 32)
        _depth30Chunk = engine.Compile(
            @"
            DEFINE epochs 10000
            DEFINE i r5
            DEFINE depth r1
            LOADC i 0
            loop:
                LOADC depth 30
                CALL recurse() depth
                FOR i epochs 1 < loop
            HALT

            recurse()
                EQ 0 r0 0
                JUMP base_case
                SUB r0 r0 1
                CALL recurse() r0
            base_case:
                RETURN r0 r0"
        );

        // Internal call
        _internalCallChunk = engine.Compile(
            @"
            DEFINE epochs 10000
            DEFINE i r2
            LOADC r1 2.0
            LOADC i 0
            loop:
                CALL internalAdd() r1
                FOR i epochs 1 < loop
            HALT

            internalAdd()
                ADD r0 r0 r0
                RETURN r0 r0"
        );

        // FFI Direct Bind
        _ffiDirectChunk = engine.Compile(
            @"
            DEFINE epochs 10000
            DEFINE i r2
            LOADC r1 2.0
            LOADC i 0
            loop:
                CALL directAdd() r1
                FOR i epochs 1 < loop
            HALT"
        );

        // FFI Typed Wrapper
        _ffiTypedChunk = engine.Compile(
            @"
            DEFINE epochs 10000
            DEFINE i r2
            LOADC r1 2.0
            LOADC i 0
            loop:
                CALL typedAdd() r1
                FOR i epochs 1 < loop
            HALT"
        );

        // FFI Fallback (reflection)
        _ffiFallbackChunk = engine.Compile(
            @"
            DEFINE epochs 10000
            DEFINE i r6
            LOADC r1 1.0
            LOADC r2 2.0
            LOADC r3 3.0
            LOADC r4 4.0
            LOADC r5 5.0
            LOADC i 0
            loop:
                CALL sumFive() r1
                FOR i epochs 1 < loop
            HALT"
        );

        _ffiDirectOverhead = engine.Compile(
            @"
            DEFINE epochs 10000
            DEFINE i r1
            LOADC i 0
            loop:
                CALL lazy() r1
                FOR i epochs 1 < loop
            HALT"
        );
    }

    [Benchmark]
    public void CallStack_Depth_10()
    {
        _vm.LoadProgram(_depth10Chunk);
        _vm.RunFast();
    }

    [Benchmark]
    public void CallStack_Depth_30()
    {
        _vm.LoadProgram(_depth30Chunk);
        _vm.RunFast();
    }

    [Benchmark]
    public void Ffi_InternalCall()
    {
        _vm.LoadProgram(_internalCallChunk);
        _vm.RunFast();
    }

    [Benchmark]
    public void Ffi_DirectBind()
    {
        _vm.LoadProgram(_ffiDirectChunk);
        _vm.RunFast();
    }

    [Benchmark]
    public void Ffi_TypedWrapper()
    {
        _vm.LoadProgram(_ffiTypedChunk);
        _vm.RunFast();
    }

    [Benchmark]
    public void Ffi_Fallback()
    {
        _vm.LoadProgram(_ffiFallbackChunk);
        _vm.RunFast();
    }

    [Benchmark]
    public void Ffi_DirectOverhead()
    {
        _vm.LoadProgram(_ffiDirectOverhead);
        _vm.RunFast();
    }

    [Benchmark]
    public void NativeDelegate() { }

    [RaptorModule]
    public static class FfiBenchmarkBindings
    {
        [RaptorMethod("typedAdd", 101)]
        public static double TypedAdd(double a) => a + a;
    }

    [RaptorModule]
    public static class FallbackBenchmarkBindings
    {
        [RaptorMethod("sumFive", 200)]
        public static double SumFive(double a, double b, double c, double d, double e) =>
            a + b + c + d + e;
    }
}
