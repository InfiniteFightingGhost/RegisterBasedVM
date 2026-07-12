using System;
using System.Collections.Generic;
using System.Linq;
using Raptor;
using Xunit;

namespace Raptor.Tests;

public class FfiAndMemoryTests
{
    [Fact]
    public void FfiAndDirectMemoryAccessTest()
    {
        VMChunk testChunk = new VMChunk();
        Assembler testAss = new(testChunk);

        testAss.RegisterHostMethod("addNumbers", 5);
        testAss.RegisterHostMethod("verifySpan", 6);
        testAss.RegisterHostMethod("verifyModifiedSpan", 7);

        string testScript =
            @"
LOADC r1 42.0
LOADC r2 58.0
CALL addNumbers() r1
PRINT r1

NEWARR r3 8
LOADC r4 0
LOADC r5 999.0
SETARR r3 r4 r5

CALL verifySpan() r3
CALL verifyModifiedSpan() r3
HALT
";

        VirtualMachine testVm = new VirtualMachine();

        testVm.RegisterHostMethod(
            5,
            (ref VMState state) =>
            {
                unsafe
                {
                    double a = state.RegPtr[0];
                    double b = state.RegPtr[1];
                    state.RegPtr[0] = a + b;
                }
            }
        );

        testVm.RegisterHostMethod(
            6,
            (ref VMState state) =>
            {
                unsafe
                {
                    double ptrVal = state.RegPtr[0];
                    Span<double> span = testVm.GetDoubleSpan(ptrVal, 1);
                    Assert.Equal(999.0, span[0]);
                    span[0] = 777.0;
                }
            }
        );

        testVm.RegisterHostMethod(
            7,
            (ref VMState state) =>
            {
                unsafe
                {
                    double ptrVal = state.RegPtr[0];
                    Span<double> span = testVm.GetDoubleSpan(ptrVal, 1);
                    Assert.Equal(777.0, span[0]);
                }
            }
        );

        testAss.Parse(testScript.Split("\n").ToList());
        BytecodeVerifier.Verify(testChunk, 1024);
        testVm.LoadProgram(testChunk);
        ExecutionResult result = testVm.RunFast();

        Assert.Equal(VMStatus.Halted, result.Status);
    }
}
