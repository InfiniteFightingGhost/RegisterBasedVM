using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Raptor;
using Xunit;

namespace Raptor.Tests;

public class BinaryFormatAndScriptEngineTests
{
    [Fact]
    public void BinaryRoundTripAndMagicBytesTest()
    {
        VMChunk rtChunk = new VMChunk();
        Assembler rtAss = new(rtChunk);
        string rtSource =
            @"
LOADC r1 42.0
LOADC r2 58.0
ADD r3 r1 r2
PRINT r3
HALT
";
        rtAss.Parse(rtSource.Split("\n").ToList());
        BytecodeVerifier.Verify(rtChunk, 1024);

        string tempPath = Path.Combine(Path.GetTempPath(), "raptor_test_roundtrip.rbc");
        try
        {
            RaptorBinary.Save(rtChunk, tempPath);

            // Verify file starts with correct magic bytes
            byte[] fileBytes = File.ReadAllBytes(tempPath);
            Assert.True(fileBytes.Length >= 20, "Binary file too small for header.");
            uint fileMagic = BitConverter.ToUInt32(fileBytes, 0);
            Assert.Equal(RaptorBinary.MagicSignature, fileMagic);

            // Load and execute
            VMChunk loadedChunk = RaptorBinary.Load(tempPath);
            Assert.Equal(rtChunk.Instructions.Length, loadedChunk.Instructions.Length);
            for (int i = 0; i < rtChunk.Instructions.Length; i++)
            {
                Assert.Equal(rtChunk.Instructions[i], loadedChunk.Instructions[i]);
            }

            VirtualMachine rtVm = new VirtualMachine();
            rtVm.LoadProgram(loadedChunk);
            ExecutionResult rtResult = rtVm.RunFast();
            Assert.Equal(VMStatus.Halted, rtResult.Status);
            Assert.Equal(100.0, rtResult.RegistersSnapshot[3]);
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }

    [Fact]
    public void InvalidMagicRejectionTest()
    {
        string badPath = Path.Combine(Path.GetTempPath(), "raptor_test_badmagic.rbc");
        try
        {
            File.WriteAllBytes(
                badPath,
                new byte[]
                {
                    0xDE, 0xAD, 0xBE, 0xEF, // Bad magic
                    0x01, 0x00, 0x00, 0x00,
                    0x00, 0x00, 0x00, 0x00,
                    0x00, 0x00, 0x00, 0x00,
                    0x00, 0x00, 0x00, 0x00,
                }
            );

            Assert.Throws<InvalidDataException>(() => RaptorBinary.Load(badPath));
        }
        finally
        {
            if (File.Exists(badPath))
                File.Delete(badPath);
        }
    }

    [Fact]
    public void ScriptEngineRunTest()
    {
        ScriptEngine engine = new ScriptEngine();
        ExecutionResult runResult = engine.Run(
            @"
LOADC r1 7.0
LOADC r2 6.0
MUL r3 r1 r2
HALT
"
        );
        Assert.Equal(VMStatus.Halted, runResult.Status);
        Assert.Equal(42.0, runResult.RegistersSnapshot[3]);
    }

    [Fact]
    public void ScriptEngineExecuteFileTest()
    {
        ScriptEngine engine = new ScriptEngine();
        string enginePath = Path.Combine(Path.GetTempPath(), "raptor_test_engine.rbc");
        try
        {
            VMChunk engineChunk = engine.Compile(
                @"
LOADC r1 123.0
LOADC r2 456.0
ADD r3 r1 r2
HALT
"
            );
            engine.SaveToFile(engineChunk, enginePath);
            ExecutionResult fileResult = engine.Execute(enginePath);
            Assert.Equal(VMStatus.Halted, fileResult.Status);
            Assert.Equal(579.0, fileResult.RegistersSnapshot[3]);
        }
        finally
        {
            if (File.Exists(enginePath))
                File.Delete(enginePath);
        }
    }

    [Fact]
    public void ScriptEngineAndFfiTest()
    {
        ScriptEngine ffiEngine = new ScriptEngine();
        ffiEngine.RegisterHostMethod(
            "double",
            0,
            (ref VMState state) =>
            {
                unsafe
                {
                    state.RegPtr[0] = state.RegPtr[0] * 2.0;
                }
            }
        );
        ExecutionResult ffiResult = ffiEngine.Run(
            @"
LOADC r1 21.0
CALL double() r1
MOVE r2 r1
HALT
"
        );
        Assert.Equal(VMStatus.Halted, ffiResult.Status);
        Assert.Equal(42.0, ffiResult.RegistersSnapshot[2]);
    }
}
