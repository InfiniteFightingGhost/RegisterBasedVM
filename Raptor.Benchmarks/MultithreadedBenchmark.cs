using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Raptor;

namespace Raptor.Benchmarks;

[MemoryDiagnoser]
public class MultithreadedBenchmark
{
    private VMChunk _physicsChunk = null!;
    private VirtualMachine[] _vms = null!;

    [GlobalSetup]
    public void Setup()
    {


        var engine = new ScriptEngine();

        const string PhysicsMovementAsm = @"
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

        _physicsChunk = engine.Compile(PhysicsMovementAsm);

        // Pre-allocate 8 separate VM instances to run concurrently without state corruption
        _vms = new VirtualMachine[8];
        for (int i = 0; i < 8; i++)
        {
            _vms[i] = new VirtualMachine();
            _vms[i].LoadProgram(_physicsChunk);
        }
    }

    [Benchmark]
    public void Multithreaded_Scale_1()
    {
        _vms[0].RunFast();
    }

    [Benchmark]
    public void Multithreaded_Scale_4()
    {
        Parallel.For(0, 4, i =>
        {
            _vms[i].RunFast();
        });
    }

    [Benchmark]
    public void Multithreaded_Scale_8()
    {
        Parallel.For(0, 8, i =>
        {
            _vms[i].RunFast();
        });
    }
}
