using System;
using System.IO;
using System.Linq;
using System.Text;
using BenchmarkDotNet.Attributes;
using Raptor;

namespace Raptor.Benchmarks;

[MemoryDiagnoser]
public class VerifierBenchmark
{
    private VMChunk _chunk100 = null!;
    private VMChunk _chunk1000 = null!;
    private VMChunk _chunk10000 = null!;
    private VMChunk _invalidJumpChunk = null!;
    private VMChunk _invalidMemoryChunk = null!;

    [GlobalSetup]
    public void Setup()
    {


        var engine = new ScriptEngine();

        _chunk100 = GenerateValidProgram(engine, 100);
        _chunk1000 = GenerateValidProgram(engine, 1000);
        _chunk10000 = GenerateValidProgram(engine, 10000);

        // Program with out-of-bounds jump target (compiled cleanly, then manually corrupted)
        _invalidJumpChunk = engine.Compile(@"
            target:
                LOADC r0 5.5
                JUMP target
                HALT");
        _invalidJumpChunk.Instructions[1] = Instruction.CreateSBx26(OpCode.JUMP, 99999);

        // Program with array allocation exceeding heap size (compiled cleanly, then manually corrupted constant pool)
        _invalidMemoryChunk = engine.Compile(@"
            DEFINE large_size 10.0
            NEWARR r1 large_size
            HALT");
        _invalidMemoryChunk.Constants[0] = 999999999.0;
    }

    private VMChunk GenerateValidProgram(ScriptEngine engine, int instructionCount)
    {
        var sb = new StringBuilder();
        // Generate valid arithmetic lines
        for (int i = 0; i < instructionCount - 2; i++)
        {
            sb.AppendLine("ADD r0 r0 r0");
        }
        sb.AppendLine("HALT");
        return engine.Compile(sb.ToString());
    }

    [Benchmark]
    public void Verifier_Scale_100()
    {
        BytecodeVerifier.Verify(_chunk100, 16 * 1024 * 1024);
    }

    [Benchmark]
    public void Verifier_Scale_1000()
    {
        BytecodeVerifier.Verify(_chunk1000, 16 * 1024 * 1024);
    }

    [Benchmark]
    public void Verifier_Scale_10000()
    {
        BytecodeVerifier.Verify(_chunk10000, 16 * 1024 * 1024);
    }

    [Benchmark]
    public void Verifier_Safety_InvalidJump()
    {
        try
        {
            BytecodeVerifier.Verify(_invalidJumpChunk, 16 * 1024 * 1024);
        }
        catch (VerificationException)
        {
            // Expected verification failure
        }
    }

    [Benchmark]
    public void Verifier_Safety_InvalidMemory()
    {
        try
        {
            BytecodeVerifier.Verify(_invalidMemoryChunk, 16 * 1024 * 1024);
        }
        catch (VerificationException)
        {
            // Expected verification failure
        }
    }
}
