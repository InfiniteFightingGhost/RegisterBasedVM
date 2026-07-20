using System;
using System.Collections.Generic;
using System.Linq;
using Raptor;
using Xunit;

namespace Raptor.Tests;

public class DisassemblerTests
{
    [Fact]
    public void DisassemblerTest()
    {
        VMChunk disChunk = new VMChunk();
        Assembler disAss = new(disChunk);
        string script =
            @"
LOADC r1 10.0
LOADC r2 5.5
ADD r3 r1 r2
FOR r4 100 1 < loop
ADD r1 r1 r1
loop:
HALT
";
        disAss.Parse(script.Split("\n").ToList());
        BytecodeVerifier.Verify(disChunk, 1024);
        string disassembly = Disassembler.Disassemble(disChunk);

        Assert.Contains("LOADC r1 10", disassembly);
        Assert.Contains("ADD r3 r1 r2", disassembly);
        Assert.Contains("FOR r4 100 1 < 0007", disassembly);
    }
}
