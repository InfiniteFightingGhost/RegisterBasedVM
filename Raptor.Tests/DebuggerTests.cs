using System;
using System.Collections.Generic;
using System.Linq;
using Raptor;
using Xunit;

namespace Raptor.Tests;

public class DebuggerTests
{
    [Fact]
    public void DebuggerHooksTest()
    {
        VMChunk dbgChunk = new VMChunk();
        Assembler dbgAss = new(dbgChunk);
        string script =
            @"
LOADC r1 10.0
LOADC r2 20.0
ADD r3 r1 r2
HALT
";
        dbgAss.Parse(script.Split("\n").ToList());
        BytecodeVerifier.Verify(dbgChunk, 1024);
        VirtualMachine vm = new VirtualMachine();
        vm.LoadProgram(dbgChunk);

        int instructionsExecuted = 0;
        vm.RunDebug(
            (ref VMState state, Instruction instruction) =>
            {
                instructionsExecuted++;
                if (instruction.Op == OpCode.ADD)
                {
                    unsafe
                    {
                        double r1Val = state.RegPtr[1];
                        double r2Val = state.RegPtr[2];
                        Assert.Equal(10.0, r1Val);
                        Assert.Equal(20.0, r2Val);
                    }
                }
            }
        );

        Assert.Equal(4, instructionsExecuted);
    }
}
