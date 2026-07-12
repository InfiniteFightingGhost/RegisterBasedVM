using System;
using System.Collections.Generic;
using System.Linq;
using Raptor;
using Xunit;

namespace Raptor.Tests;

public class PanicDumpTests
{
    [Fact]
    public void DivisionByZeroPanicDumpTest()
    {
        VMChunk chunkDivZero = new VMChunk();
        Assembler assDiv = new(chunkDivZero);
        string script =
            @"
LOADC r1 10.0
LOADC r2 0.0
DIV r3 r1 r2
HALT
";
        assDiv.Parse(script.Split("\n").ToList());
        BytecodeVerifier.Verify(chunkDivZero, 1024);
        VirtualMachine vm = new VirtualMachine();
        vm.LoadProgram(chunkDivZero);
        ExecutionResult result = vm.RunFast();

        Assert.Equal(VMStatus.DivisionByZero, result.Status);
        Assert.Equal(10.0, result.RegistersSnapshot[1]);
        Assert.Equal(0.0, result.RegistersSnapshot[2]);
    }

    [Fact]
    public void HeapOutOfMemoryPanicDumpTest()
    {
        VMChunk chunkOom = new VMChunk();
        Assembler assOom = new(chunkOom);
        string script =
            @"
LOADC r1 20000000.0
NEWARR r2 r1
HALT
";
        assOom.Parse(script.Split("\n").ToList());
        BytecodeVerifier.Verify(chunkOom, 1024);
        VirtualMachine vm = new VirtualMachine();
        vm.LoadProgram(chunkOom);
        ExecutionResult result = vm.RunFast();

        Assert.Equal(VMStatus.OutOfMemory, result.Status);
        Assert.Equal(20000000.0, result.RegistersSnapshot[1]);
    }

    [Fact]
    public void CallStackOverflowPanicDumpTest()
    {
        VMChunk chunkOverflow = new VMChunk();
        Assembler assOverflow = new(chunkOverflow);

        string script =
            @"
CALL recCall() r0
HALT
recCall()
CALL recCall() r0
HALT
";
        assOverflow.Parse(script.Split("\n").ToList());

        BytecodeVerifier.Verify(chunkOverflow, 1024);
        VirtualMachine vm = new VirtualMachine();
        vm.LoadProgram(chunkOverflow);
        ExecutionResult result = vm.RunFast();

        Assert.Equal(VMStatus.StackOverflow, result.Status);
        Assert.Equal(32, result.CallStackSnapshot.Length);
    }
}
