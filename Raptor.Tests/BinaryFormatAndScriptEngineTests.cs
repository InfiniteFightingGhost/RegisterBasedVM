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

    [Fact]
    public void ScriptEngineCompileFileTest()
    {
        ScriptEngine engine = new ScriptEngine();
        string rasmPath = Path.Combine(Path.GetTempPath(), "test_script.rasm");
        try
        {
            File.WriteAllText(rasmPath, @"
LOADC r1 10.0
LOADC r2 5.0
SUB r3 r1 r2
HALT
");
            VMChunk chunk = engine.CompileFile(rasmPath);
            ExecutionResult result = engine.Execute(chunk);
            Assert.Equal(VMStatus.Halted, result.Status);
            Assert.Equal(5.0, result.RegistersSnapshot[3]);
        }
        finally
        {
            if (File.Exists(rasmPath))
                File.Delete(rasmPath);
        }
    }

    [Fact]
    public void ScriptEngineRunFileTest()
    {
        ScriptEngine engine = new ScriptEngine();
        string rasmPath = Path.Combine(Path.GetTempPath(), "test_run_script.rasm");
        try
        {
            File.WriteAllText(rasmPath, @"
LOADC r1 8.0
LOADC r2 9.0
MUL r3 r1 r2
HALT
");
            ExecutionResult result = engine.RunFile(rasmPath);
            Assert.Equal(VMStatus.Halted, result.Status);
            Assert.Equal(72.0, result.RegistersSnapshot[3]);
        }
        finally
        {
            if (File.Exists(rasmPath))
                File.Delete(rasmPath);
        }
    }

    [Fact]
    public void ScriptWatcherHotReloadTest()
    {
        ScriptEngine engine = new ScriptEngine();
        string rasmPath = Path.Combine(Path.GetTempPath(), "test_hotreload.rasm");
        try
        {
            File.WriteAllText(rasmPath, @"
LOADC r1 10.0
HALT
");
            using ScriptWatcher watcher = new ScriptWatcher(engine, rasmPath);
            
            // Execute initial version
            ExecutionResult result1 = engine.Execute(watcher.ActiveChunk);
            Assert.Equal(10.0, result1.RegistersSnapshot[1]);

            // Set up tracking events
            bool reloadedFired = false;
            watcher.OnReloaded += (chunk) => reloadedFired = true;

            // Modify the script file
            File.WriteAllText(rasmPath, @"
LOADC r1 20.0
HALT
");

            // Wait a brief moment for the file watcher to detect, compile, and reload
            for (int i = 0; i < 50; i++)
            {
                if (reloadedFired) break;
                System.Threading.Thread.Sleep(10);
            }

            Assert.True(reloadedFired, "Reload event did not fire.");

            // Execute reloaded version
            ExecutionResult result2 = engine.Execute(watcher.ActiveChunk);
            Assert.Equal(20.0, result2.RegistersSnapshot[1]);
        }
        finally
        {
            if (File.Exists(rasmPath))
                File.Delete(rasmPath);
        }
    }

    [Fact]
    public void RaptorScriptCompilerTest()
    {
        string raptorScript = @"
var x = 10.0;
var y = 5.0;
x += y;        // Desugars to x = x + y -> x = 15.0
x++;           // Desugars to x = x + 1 -> x = 16.0

var result = 0.0;
if (x > 15.0) {
    result = x * 2.0; // 16.0 * 2.0 = 32.0
} else {
    result = 0.0;
}
";
        string rasmCode = Raptor.Compiler.RaptorScriptCompiler.Compile(raptorScript, out var variables);

        ScriptEngine engine = new ScriptEngine();
        VMChunk chunk = engine.Compile(rasmCode);
        ExecutionResult runResult = engine.Execute(chunk);

        Assert.Equal(VMStatus.Halted, runResult.Status);
        
        // Look up the register for variable 'result' dynamically
        int resultReg = variables["result"];
        Assert.Equal(32.0, runResult.RegistersSnapshot[resultReg]);
    }

    [Fact]
    public void SourceMap_TranslateError_MapsRuntimeExceptionsCorrectly()
    {
        string raptorScript = @"
var x = 10.0;
var y = 0.0;
var result = x / y; // Division by zero!
";
        string rasmCode = Raptor.Compiler.RaptorScriptCompiler.Compile(raptorScript);
        
        ScriptEngine engine = new ScriptEngine();
        VMChunk chunk = engine.Compile(rasmCode);
        ExecutionResult runResult = engine.Execute(chunk);

        Assert.Equal(VMStatus.DivisionByZero, runResult.Status);
        
        string errorDetails = ScriptEngine.TranslateError(chunk, runResult.IpOffset, raptorScript);
        
        Assert.Contains("Runtime error at line 4", errorDetails);
        Assert.Contains("var result = x / y;", errorDetails);
    }

    [Fact]
    public void TestGameplayCompile()
    {
        string raptorScript = @"
// Assets/Scripts/enemy_ai.rapt
var targetDistance = enemy.getDistanceToPlayer();
var state = enemy.getState(); // 0 = Idle, 1 = Chasing, 2 = Attacking

if (targetDistance < 5.0) {
    enemy.setState(2.0);
    var cooldown = enemy.getAttackCooldown();
    if (cooldown == 0.0) {
        enemy.attackPlayer(15.0);
        enemy.setAttackCooldown(3.0);
    } else {
        cooldown -= 0.016; // subtract delta
        if (cooldown < 0.0) { cooldown = 0.0; }
        enemy.setAttackCooldown(cooldown);
    }
} else {
    if (targetDistance < 20.0) {
        enemy.setState(1.0);
        enemy.moveTowardsPlayer(8.0);
    } else {
        enemy.setState(0.0);
        enemy.patrol(3.0);
    }
}
";
        string rasm = Raptor.Compiler.RaptorScriptCompiler.Compile(raptorScript);
        Assert.NotEmpty(rasm);
        System.IO.File.WriteAllText("/home/andy/.gemini/antigravity/brain/ec45c6eb-d535-4e23-909e-6974cf17074d/scratch/compiled_gameplay.rasm", rasm);
        
        ScriptEngine engine = new ScriptEngine();
        // Register some dummy FFI methods so the assembler doesn't fail on missing names
        var table = new FFIHostTable();
        table.Register("enemy.getDistanceToPlayer", 0, (ref VMState s) => {});
        table.Register("enemy.getState", 1, (ref VMState s) => {});
        table.Register("enemy.setState", 2, (ref VMState s) => {});
        table.Register("enemy.getAttackCooldown", 3, (ref VMState s) => {});
        table.Register("enemy.setAttackCooldown", 4, (ref VMState s) => {});
        table.Register("enemy.attackPlayer", 5, (ref VMState s) => {});
        table.Register("enemy.moveTowardsPlayer", 6, (ref VMState s) => {});
        table.Register("enemy.patrol", 7, (ref VMState s) => {});
        engine.RegisterHostTable(table);
        
        VMChunk chunk = engine.Compile(rasm);
        Assert.NotEmpty(chunk.Instructions);
    }
}
