using Raptor;

// --- FFI & MEMORY ACCESS UNIT TEST ---
Console.Error.WriteLine("Running Phase 3 FFI and Direct Memory Access Unit Tests...");
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
            if (span[0] != 999.0)
            {
                throw new Exception($"verifySpan failed: Expected 999.0, got {span[0]}");
            }
            Console.Error.WriteLine("Span verify success: raw memory contains " + span[0]);
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
            if (span[0] != 777.0)
            {
                throw new Exception($"verifyModifiedSpan failed: Expected 777.0, got {span[0]}");
            }
            Console.Error.WriteLine("Span verify success: modified raw memory contains " + span[0]);
        }
    }
);

testAss.Parse(testScript.Split("\n").ToList());
BytecodeVerifier.Verify(testChunk, 1024);
testVm.LoadProgram(testChunk);
testVm.RunFast();
Console.Error.WriteLine("Phase 3 FFI and Direct Memory Access Unit Tests PASSED.");

// --- PHASE 5: PANIC DUMP UNIT TESTS ---
Console.Error.WriteLine("Running Phase 5 Panic Dump Unit Tests...");

// 1. Division by Zero Test
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
    if (result.Status != VMStatus.DivisionByZero)
    {
        throw new Exception(
            $"Panic Dump check failed: Expected DivisionByZero, got {result.Status}"
        );
    }
    if (result.RegistersSnapshot[1] != 10.0 || result.RegistersSnapshot[2] != 0.0)
    {
        throw new Exception("Panic Dump check failed: Register snapshot values are incorrect.");
    }
    Console.Error.WriteLine(
        "Division by zero panic dump: PASSED (IpOffset: " + result.IpOffset + ")"
    );
}

// 2. Heap Out of Memory Test
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
    if (result.Status != VMStatus.OutOfMemory)
    {
        throw new Exception($"Panic Dump check failed: Expected OutOfMemory, got {result.Status}");
    }
    if (result.RegistersSnapshot[1] != 20000000.0)
    {
        throw new Exception("Panic Dump check failed: Register snapshot values are incorrect.");
    }
    Console.Error.WriteLine("OutOfMemory panic dump: PASSED (IpOffset: " + result.IpOffset + ")");
}

// 3. Call Stack Overflow Test
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
    if (result.Status != VMStatus.StackOverflow)
    {
        throw new Exception(
            $"Panic Dump check failed: Expected StackOverflow, got {result.Status}"
        );
    }
    if (result.CallStackSnapshot.Length != 32)
    {
        throw new Exception(
            $"Panic Dump check failed: Expected 32 frames, got {result.CallStackSnapshot.Length}"
        );
    }
    Console.Error.WriteLine(
        "StackOverflow panic dump: PASSED (Frames captured: "
            + result.CallStackSnapshot.Length
            + ")"
    );
}

// --- PHASE 5: DISASSEMBLER UNIT TESTS ---
Console.Error.WriteLine("Running Phase 5 Disassembler Unit Tests...");
{
    VMChunk disChunk = new VMChunk();
    Assembler disAss = new(disChunk);
    string script =
        @"
LOADC r1 10.0
LOADC r2 5.5
ADD r3 r1 r2
FOR r4 100 1 < loop
PRINT r3
loop:
HALT
";
    disAss.Parse(script.Split("\n").ToList());
    BytecodeVerifier.Verify(disChunk, 1024);
    string disassembly = VirtualMachine.Disassemble(disChunk);
    Console.Error.WriteLine("Disassembly result:\n" + disassembly);
    if (
        !disassembly.Contains("LOADC r1 10")
        || !disassembly.Contains("ADD r3 r1 r2")
        || !disassembly.Contains("FOR r4 100 1 < 0007")
    )
    {
        throw new Exception("Disassembler test failed: output formatting is incorrect.");
    }
    Console.Error.WriteLine("Disassembler test: PASSED");
}

// --- PHASE 5: DEBUGGER HOOKS UNIT TESTS ---
Console.Error.WriteLine("Running Phase 5 Debugger Hooks Unit Tests...");
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
                    if (r1Val != 10.0 || r2Val != 20.0)
                    {
                        throw new Exception(
                            $"Debugger hook inspection failed: expected r1=10, r2=20, got r1={r1Val}, r2={r2Val}"
                        );
                    }
                }
            }
        }
    );

    if (instructionsExecuted != 4)
    {
        throw new Exception(
            $"Debugger hook count failed: expected 4 instructions, executed {instructionsExecuted}"
        );
    }
    Console.Error.WriteLine("Debugger Hooks test: PASSED");
}

// --- PHASE 6: BINARY FORMAT & SCRIPT ENGINE UNIT TESTS ---
Console.Error.WriteLine("Running Phase 6 Binary Format Unit Tests...");
{
    // 1. Binary round-trip test: compile → save → load → execute → verify
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
        if (fileBytes.Length < 20)
            throw new Exception("Binary file too small for header.");
        uint fileMagic = BitConverter.ToUInt32(fileBytes, 0);
        if (fileMagic != RaptorBinary.MagicSignature)
            throw new Exception(
                $"Magic signature mismatch: expected 0x{RaptorBinary.MagicSignature:X8}, got 0x{fileMagic:X8}."
            );

        // Load and execute
        VMChunk loadedChunk = RaptorBinary.Load(tempPath);
        if (loadedChunk.Instructions.Length != rtChunk.Instructions.Length)
            throw new Exception(
                $"Instruction count mismatch: expected {rtChunk.Instructions.Length}, got {loadedChunk.Instructions.Length}."
            );
        for (int i = 0; i < rtChunk.Instructions.Length; i++)
        {
            if (loadedChunk.Instructions[i] != rtChunk.Instructions[i])
                throw new Exception($"Instruction mismatch at index {i}.");
        }

        VirtualMachine rtVm = new VirtualMachine();
        rtVm.LoadProgram(loadedChunk);
        ExecutionResult rtResult = rtVm.RunFast();
        if (rtResult.Status != VMStatus.Halted)
            throw new Exception($"Round-trip execution failed with status: {rtResult.Status}.");
        // r3 should contain 100.0 (42 + 58)
        if (rtResult.RegistersSnapshot[3] != 100.0)
            throw new Exception(
                $"Round-trip result mismatch: expected r3=100, got r3={rtResult.RegistersSnapshot[3]}."
            );

        Console.Error.WriteLine("Binary round-trip test: PASSED");
    }
    finally
    {
        if (File.Exists(tempPath))
            File.Delete(tempPath);
    }

    // 2. Invalid magic rejection test
    string badPath = Path.Combine(Path.GetTempPath(), "raptor_test_badmagic.rbc");
    try
    {
        File.WriteAllBytes(
            badPath,
            new byte[]
            {
                0xDE,
                0xAD,
                0xBE,
                0xEF,
                0x01,
                0x00,
                0x00,
                0x00,
                0x00,
                0x00,
                0x00,
                0x00,
                0x00,
                0x00,
                0x00,
                0x00,
                0x00,
                0x00,
                0x00,
                0x00,
            }
        );
        bool caughtException = false;
        try
        {
            RaptorBinary.Load(badPath);
        }
        catch (InvalidDataException)
        {
            caughtException = true;
        }
        if (!caughtException)
            throw new Exception(
                "Invalid magic test failed: expected InvalidDataException was not thrown."
            );
        Console.Error.WriteLine("Invalid magic rejection test: PASSED");
    }
    finally
    {
        if (File.Exists(badPath))
            File.Delete(badPath);
    }
}

Console.Error.WriteLine("Running Phase 6 ScriptEngine Unit Tests...");
{
    // 3. ScriptEngine.Run test: compile + execute in one call
    ScriptEngine engine = new ScriptEngine();
    ExecutionResult runResult = engine.Run(
        @"
LOADC r1 7.0
LOADC r2 6.0
MUL r3 r1 r2
HALT
"
    );
    if (runResult.Status != VMStatus.Halted)
        throw new Exception($"ScriptEngine.Run failed with status: {runResult.Status}.");
    if (runResult.RegistersSnapshot[3] != 42.0)
        throw new Exception(
            $"ScriptEngine.Run result mismatch: expected r3=42, got r3={runResult.RegistersSnapshot[3]}."
        );
    Console.Error.WriteLine("ScriptEngine.Run test: PASSED");

    // 4. ScriptEngine.Execute(filePath) test: save binary → load + execute via engine
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
        if (fileResult.Status != VMStatus.Halted)
            throw new Exception(
                $"ScriptEngine.Execute(file) failed with status: {fileResult.Status}."
            );
        if (fileResult.RegistersSnapshot[3] != 579.0)
            throw new Exception(
                $"ScriptEngine.Execute(file) result mismatch: expected r3=579, got r3={fileResult.RegistersSnapshot[3]}."
            );
        Console.Error.WriteLine("ScriptEngine.Execute(filePath) test: PASSED");
    }
    finally
    {
        if (File.Exists(enginePath))
            File.Delete(enginePath);
    }

    // 5. ScriptEngine + FFI test
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
    if (ffiResult.Status != VMStatus.Halted)
        throw new Exception($"ScriptEngine FFI test failed with status: {ffiResult.Status}.");
    if (ffiResult.RegistersSnapshot[2] != 42.0)
        throw new Exception(
            $"ScriptEngine FFI test result mismatch: expected r2=42, got r2={ffiResult.RegistersSnapshot[2]}."
        );
    Console.Error.WriteLine("ScriptEngine + FFI test: PASSED");
}

Console.Error.WriteLine("All Phase 6 tests PASSED.");

// Console.Error.WriteLine($"Compiled {chunk.Instructions?.Length ?? 0} instructions.");
//
// // Verify compiled program
// try
// {
//     BytecodeVerifier.(chunk, 16 * 1024 * 1024);
//     Console.Error.WriteLine("RayTracer verification PASSED.");
// }
// catch (Exception ex)
// {
//     Console.Error.WriteLine($"RayTracer verification FAILED: {ex.Message}");
//     throw;
// }
//
// // Run verifier unit tests
// RunVerifierUnitTests();

// int sinIndex = Array.IndexOf(chunk.Constants, -999.123);
// int cosIndex = Array.IndexOf(chunk.Constants, -999.456);
// int camXIndex = Array.IndexOf(chunk.Constants, -999.789);
// int camYIndex = Array.IndexOf(chunk.Constants, -999.012);
// int camZIndex = Array.IndexOf(chunk.Constants, -999.345);
//
// if (sinIndex == -1 || cosIndex == -1 || camXIndex == -1 || camYIndex == -1 || camZIndex == -1)
// {
//     throw new Exception("Could not find the unique dummy constants in the compiled pool!");
// }
//
// int totalFrames = 30;
var originalOut = Console.Out;

//
// for (int frame = 0; frame < totalFrames; frame++)
// {
//     double theta = 2.0 * Math.PI * frame / totalFrames;
//     double sinTheta = Math.Sin(theta);
//     double cosTheta = Math.Cos(theta);
//
//     // Orbit parameters: radius 3.5 around center (0, 0, 3)
//     double radiusVal = 3.5;
//     double camX = radiusVal * sinTheta;
//     double camY = 0.0;
//     double camZ = 3.0 - radiusVal * cosTheta;
//
//     // Overwrite the constants in the chunk
//     chunk.Constants[sinIndex] = sinTheta;
//     chunk.Constants[cosIndex] = cosTheta;
//     chunk.Constants[camXIndex] = camX;
//     chunk.Constants[camYIndex] = camY;
//     chunk.Constants[camZIndex] = camZ;
//
//     // Redirect standard output to frame_xx.ppm
//     var sw = System.Diagnostics.Stopwatch.StartNew();
//     using (var writer = new System.IO.StreamWriter($"frame_{frame:D2}.ppm"))
//     {
//         Console.SetOut(writer);
//
//         VirtualMachine vm = new();
//         vm.LoadProgram(chunk);
//         vm.RunFast();
//     }
//     Console.Error.WriteLine($"Frame {frame:D2} rendered in {sw.ElapsedMilliseconds} ms");
// }

// Restore standard output
Console.SetOut(originalOut);
Console.Error.WriteLine("Successfully rendered 30 frames!");
