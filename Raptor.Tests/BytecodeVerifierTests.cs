using Raptor;
using Xunit;

namespace Raptor.Tests;

public class BytecodeVerifierTests
{
    [Fact]
    public void BytecodeVerifierThrowsVerificationExceptionForEmptyInstructions()
    {
        VMChunk badChunk = new VMChunk();
        Assert.Throws<VerificationException>(() => BytecodeVerifier.Verify(badChunk, 1024));
    }

    [Fact]
    public void BytecodeVerifierThrowsVerificationExceptionForMissingTerminatingInstruction()
    {
        VMChunk badChunk = new VMChunk();
        badChunk.Instructions = new uint[] { Instruction.CreateABC(OpCode.ADD, 0, 0, 0) };
        Assert.Throws<VerificationException>(() => BytecodeVerifier.Verify(badChunk, 1024));
    }

    [Fact]
    public void BytecodeVerifierThrowsVerificationExceptionForInvalidJumpOutOfBounds()
    {
        VMChunk badChunk = new VMChunk();
        badChunk.Instructions = new uint[]
        {
            Instruction.CreateSBx26(OpCode.JUMP, 10),
            Instruction.CreateABC(OpCode.HALT, 0, 0, 0),
        };
        Assert.Throws<VerificationException>(() => BytecodeVerifier.Verify(badChunk, 1024));
    }

    [Fact]
    public void BytecodeVerifierThrowsVerificationExceptionForJumpInMiddleOfAFORInstruction()
    {
        VMChunk badChunk = new VMChunk();
        badChunk.Instructions = new uint[]
        {
            Instruction.CreateSBx26(OpCode.JUMP, 2),
            Instruction.CreateABC(OpCode.FOR, 0, 0, 0),
            Instruction.CreateAsBx(OpCode.FOR, 0, 0),
            Instruction.CreateABC(OpCode.HALT, 0, 0, 0),
        };
        Assert.Throws<VerificationException>(() => BytecodeVerifier.Verify(badChunk, 1024));
    }

    [Fact]
    public void BytecodeVerifierThrowsVerificationExceptionForIndexOutOfBounds()
    {
        VMChunk badChunk = new VMChunk();
        var property = typeof(VMChunk).GetProperty("Constants");
        var setter = property?.GetSetMethod(true);
        setter?.Invoke(badChunk, new object[] { Array.Empty<double>() });

        badChunk.Instructions = new uint[]
        {
            Instruction.CreateABC(OpCode.ADD, 0, 256, 0),
            Instruction.CreateABC(OpCode.HALT, 0, 0, 0),
        };
        Assert.Throws<VerificationException>(() => BytecodeVerifier.Verify(badChunk, 1024));
    }

    [Fact]
    public void BytecodeVerifierThrowsVerificationExceptionForIncompleteFORInstruction()
    {
        VMChunk badChunk = new VMChunk();
        badChunk.Instructions = new uint[] { Instruction.CreateABC(OpCode.FOR, 0, 0, 0) };
        Assert.Throws<VerificationException>(() => BytecodeVerifier.Verify(badChunk, 1024));
    }

    [Fact]
    public void BytecodeVerifierValidatesProgram()
    {
        VMChunk goodChunk = new VMChunk();
        goodChunk.Instructions = new uint[] { Instruction.CreateABC(OpCode.HALT, 0, 0, 0) };
        // Verification should run successfully without throwing any exceptions
        BytecodeVerifier.Verify(goodChunk, 1024);
    }
}
