using System;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Attributes;
using Raptor;

namespace Raptor.Benchmarks;

[MemoryDiagnoser]
public class MemoryBenchmark
{
    private VirtualMachine _vm = null!;
    private VMChunk _arrayAccessChunk = null!;
    private VMChunk _allocDeallocCleanChunk = null!;
    private VMChunk _allocDeallocChurnChunk = null!;

    [GlobalSetup]
    public void Setup()
    {


        _vm = new VirtualMachine();
        var engine = new ScriptEngine();

        // 1. Benchmark memory read/write access
        _arrayAccessChunk = engine.Compile(@"
            DEFINE size 1000
            DEFINE epochs 10
            DEFINE arr r1
            DEFINE i r2
            DEFINE val r3
            DEFINE outer r4
            NEWARR arr size
            LOADC outer 0
            outer_loop:
                LOADC i 0
                loop:
                    SETARR arr i i
                    GETARR val arr i
                    FOR i size 1 < loop
                FOR outer epochs 1 < outer_loop
            FREEARR arr
            HALT");

        // 2. Best-case allocation/deallocation (constant sizing)
        _allocDeallocCleanChunk = engine.Compile(@"
            DEFINE epochs 1000
            DEFINE i r2
            DEFINE arr r1
            LOADC i 0
            loop:
                NEWARR arr 32
                FREEARR arr
                FOR i epochs 1 < loop
            HALT");

        // 3. Out-of-order churn (variable sizing, creating free list traversal overhead)
        _allocDeallocChurnChunk = engine.Compile(@"
            DEFINE epochs 300
            DEFINE i r5
            DEFINE arr1 r1
            DEFINE arr2 r2
            DEFINE arr3 r3
            DEFINE arr4 r4
            LOADC i 0
            loop:
                NEWARR arr1 16
                NEWARR arr2 32
                NEWARR arr3 48
                NEWARR arr4 64
                
                ; Free in fragmented order to populate free list
                FREEARR arr2
                FREEARR arr4
                FREEARR arr1
                FREEARR arr3
                
                FOR i epochs 1 < loop
            HALT");
    }

    [Benchmark]
    public void Memory_ArrayAccess()
    {
        _vm.LoadProgram(_arrayAccessChunk);
        _vm.RunFast();
    }

    [Benchmark]
    public void Memory_AllocDeallocClean()
    {
        _vm.LoadProgram(_allocDeallocCleanChunk);
        _vm.RunFast();
    }

    [Benchmark]
    public void Memory_AllocDeallocChurn()
    {
        _vm.LoadProgram(_allocDeallocChurnChunk);
        _vm.RunFast();
    }
}
