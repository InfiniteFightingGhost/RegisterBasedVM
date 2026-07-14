using System.Reflection;
using Raptor;
using Raptor.Attributes;
using Xunit;

namespace Raptor.Tests;

// --------------------------------------------------------------
//  Test Modules (used by multiple tests)
// --------------------------------------------------------------

[RaptorModule]
public class StaticDirectBindModule
{
    [RaptorMethod("doubleR0", 100)]
    public static void DoubleR0(ref VMState state)
    {
        unsafe
        {
            state.RegPtr[0] = state.RegPtr[0] * 2.0;
        }
    }
}

[RaptorModule]
public class InstanceDirectBindModule
{
    private readonly double _multiplier;

    public InstanceDirectBindModule(double multiplier) => _multiplier = multiplier;

    [RaptorMethod("multiply", 101)]
    public void Multiply(ref VMState state)
    {
        unsafe
        {
            state.RegPtr[0] = state.RegPtr[0] * _multiplier;
        }
    }
}

[RaptorModule("math")]
public class PrefixedModule
{
    [RaptorMethod]
    public static void Add(ref VMState state)
    {
        unsafe
        {
            state.RegPtr[0] = state.RegPtr[0] + state.RegPtr[1];
        }
    }
}

[RaptorModule]
public class AutoNameModule
{
    [RaptorMethod]
    public static void SpawnEnemy(ref VMState state)
    {
        unsafe
        {
            state.RegPtr[0] = 999.0;
        }
    }
}

[RaptorModule]
public class IgnoreModule
{
    [RaptorMethod]
    public static void Visible(ref VMState state)
    {
        unsafe
        {
            state.RegPtr[0] = 1.0;
        }
    }

    [RaptorIgnore]
    [RaptorMethod]
    public static void Hidden(ref VMState state)
    {
        unsafe
        {
            state.RegPtr[0] = -1.0;
        }
    }
}

[RaptorModule]
public class MetadataModule
{
    [RaptorMethod("documented")]
    [RaptorDescription("Adds r0 and r1, stores result in r0")]
    [RaptorPure]
    public static void DocumentedAdd([RaptorParam("First operand")] ref VMState state)
    {
        unsafe
        {
            state.RegPtr[0] = state.RegPtr[0] + state.RegPtr[1];
        }
    }
}

[RaptorModule]
public class TypedWrapperDoubleModule
{
    [RaptorMethod("addDoubles", 102)]
    public static double AddDoubles(double a, double b) => a + b;
}

[RaptorModule]
public class TypedWrapperIntModule
{
    [RaptorMethod("multiplyInts", 103)]
    public static int MultiplyInts(int a, int b) => a * b;
}

[RaptorModule]
public class TypedWrapperBoolModule
{
    [RaptorMethod("isPositive", 104)]
    public static bool IsPositive(double x) => x > 0.0;
}

[RaptorModule]
public class TypedWrapperVoidModule
{
    public static double CapturedValue;

    [RaptorMethod("setFlag", 105)]
    public static void SetFlag(double value) => CapturedValue = value;
}

[RaptorModule]
public class InstanceTypedWrapperModule
{
    public double LastResult { get; private set; }

    [RaptorMethod("computeInstance", 106)]
    public double Compute(double a, double b)
    {
        LastResult = a * b;
        return LastResult;
    }
}

public class DuplicateNameModuleA
{
    [RaptorMethod("conflict")]
    public static void MethodA(ref VMState state) { }
}

public class DuplicateNameModuleB
{
    [RaptorMethod("conflict")]
    public static void MethodB(ref VMState state) { }
}

public class DuplicateIndexModuleA
{
    [RaptorMethod("first", 5)]
    public static void MethodA(ref VMState state) { }
}

public class DuplicateIndexModuleB
{
    [RaptorMethod("second", 5)]
    public static void MethodB(ref VMState state) { }
}

public class UnsupportedTypeModule
{
    [RaptorMethod]
    public static string BadReturn(double x) => x.ToString();
}

public class UnsupportedParamModule
{
    [RaptorMethod]
    public static double BadParam(string name) => 0.0;
}

[RaptorModule]
public class FallbackPathModule
{
    [RaptorMethod("sumFive", 200)]
    public static double SumFive(double a, double b, double c, double d, double e) =>
        a + b + c + d + e;
}

// ──────────────────────────────────────────────────────────────
//  Tests
// ──────────────────────────────────────────────────────────────

public class FfiReflectionTests
{
    // ── Direct Bind Tests ──────────────────────────────────

    [Fact]
    public void StaticDirectBind_RegistersAndExecutes()
    {
        var table = new FFIHostTable();
        table.RegisterModule<StaticDirectBindModule>();

        Assert.True(table.Methods.ContainsKey("doubleR0"));
        Assert.Equal((ushort)100, table.Methods["doubleR0"].Index);

        ScriptEngine engine = new ScriptEngine();
        engine.RegisterHostTable(table);
        ExecutionResult result = engine.Run(
            @"
LOADC r1 21.0
CALL doubleR0() r1
MOVE r2 r1
HALT
"
        );
        Assert.Equal(VMStatus.Halted, result.Status);
        Assert.Equal(42.0, result.RegistersSnapshot[2]);
    }

    [Fact]
    public void InstanceDirectBind_CapturesInstance()
    {
        var module = new InstanceDirectBindModule(3.0);
        var table = new FFIHostTable();
        table.RegisterModule(module);

        Assert.True(table.Methods.ContainsKey("multiply"));

        ScriptEngine engine = new ScriptEngine();
        engine.RegisterHostTable(table);
        ExecutionResult result = engine.Run(
            @"
LOADC r1 10.0
CALL multiply() r1
MOVE r2 r1
HALT
"
        );
        Assert.Equal(VMStatus.Halted, result.Status);
        Assert.Equal(30.0, result.RegistersSnapshot[2]);
    }

    // ── Typed Wrapper Tests ───────────────────────────────

    [Fact]
    public void TypedWrapper_Doubles_ReturnInR0()
    {
        var table = new FFIHostTable();
        table.RegisterModule<TypedWrapperDoubleModule>();

        ScriptEngine engine = new ScriptEngine();
        engine.RegisterHostTable(table);
        ExecutionResult result = engine.Run(
            @"
LOADC r1 21.0
LOADC r2 21.0
CALL addDoubles() r1
MOVE r3 r1
HALT
"
        );
        Assert.Equal(VMStatus.Halted, result.Status);
        Assert.Equal(42.0, result.RegistersSnapshot[3]);
    }

    [Fact]
    public void TypedWrapper_Ints_CastRoundTrip()
    {
        var table = new FFIHostTable();
        table.RegisterModule<TypedWrapperIntModule>();

        ScriptEngine engine = new ScriptEngine();
        engine.RegisterHostTable(table);
        ExecutionResult result = engine.Run(
            @"
LOADC r1 6.0
LOADC r2 7.0
CALL multiplyInts() r1
MOVE r3 r1
HALT
"
        );
        Assert.Equal(VMStatus.Halted, result.Status);
        Assert.Equal(42.0, result.RegistersSnapshot[3]);
    }

    [Fact]
    public void TypedWrapper_Bool_Conversion()
    {
        var table = new FFIHostTable();
        table.RegisterModule<TypedWrapperBoolModule>();

        ScriptEngine engine = new ScriptEngine();
        engine.RegisterHostTable(table);
        ExecutionResult result = engine.Run(
            @"
LOADC r1 5.0
CALL isPositive() r1
MOVE r2 r1
HALT
"
        );
        Assert.Equal(VMStatus.Halted, result.Status);
        Assert.Equal(1.0, result.RegistersSnapshot[2]); // true → 1.0
    }

    [Fact]
    public void TypedWrapper_VoidReturn_NoWriteBack()
    {
        TypedWrapperVoidModule.CapturedValue = 0.0;
        var table = new FFIHostTable();
        table.RegisterModule<TypedWrapperVoidModule>();

        ScriptEngine engine = new ScriptEngine();
        engine.RegisterHostTable(table);
        ExecutionResult result = engine.Run(
            @"
LOADC r1 123.0
CALL setFlag() r1
HALT
"
        );
        Assert.Equal(VMStatus.Halted, result.Status);
        Assert.Equal(123.0, TypedWrapperVoidModule.CapturedValue);
    }

    [Fact]
    public void TypedWrapper_InstanceMethod_CapturesThis()
    {
        var module = new InstanceTypedWrapperModule();
        var table = new FFIHostTable();
        table.RegisterModule(module);

        ScriptEngine engine = new ScriptEngine();
        engine.RegisterHostTable(table);
        ExecutionResult result = engine.Run(
            @"
LOADC r1 6.0
LOADC r2 7.0
CALL computeInstance() r1
MOVE r3 r1
HALT
"
        );
        Assert.Equal(VMStatus.Halted, result.Status);
        Assert.Equal(42.0, result.RegistersSnapshot[3]);
        Assert.Equal(42.0, module.LastResult);
    }

    // ── Auto Name / Auto Index Tests ─────────────────────

    [Fact]
    public void AutoCamelCaseName_SpawnEnemy()
    {
        var table = new FFIHostTable();
        table.RegisterModule<AutoNameModule>();

        Assert.True(table.Methods.ContainsKey("spawnEnemy"));
        Assert.False(table.Methods.ContainsKey("SpawnEnemy"));
    }

    [Fact]
    public void AutoIndex_SequentialAssignment()
    {
        var table = new FFIHostTable();
        table.RegisterModule<AutoNameModule>();
        table.RegisterModule<IgnoreModule>(); // has "visible"

        var indices = table.Methods.Values.Select(m => m.Index).OrderBy(i => i).ToList();
        Assert.Equal(new List<ushort> { 0, 1 }, indices);
    }

    // ── Module Prefix Test ────────────────────────────────

    [Fact]
    public void ModulePrefix_PrependsDotSeparated()
    {
        var table = new FFIHostTable();
        table.RegisterModule<PrefixedModule>();

        Assert.True(table.Methods.ContainsKey("math.add"));
        Assert.False(table.Methods.ContainsKey("add"));
    }

    [Fact]
    public void ModulePrefix_EndToEnd()
    {
        var table = new FFIHostTable();
        table.RegisterModule<PrefixedModule>();

        ScriptEngine engine = new ScriptEngine();
        engine.RegisterHostTable(table);
        ExecutionResult result = engine.Run(
            @"
LOADC r1 20.0
LOADC r2 22.0
CALL math.add() r1
MOVE r3 r1
HALT
"
        );
        Assert.Equal(VMStatus.Halted, result.Status);
        Assert.Equal(42.0, result.RegistersSnapshot[3]);
    }

    // ── Ignore Test ───────────────────────────────────────

    [Fact]
    public void RaptorIgnore_ExcludesMethod()
    {
        var table = new FFIHostTable();
        table.RegisterModule<IgnoreModule>();

        Assert.True(table.Methods.ContainsKey("visible"));
        Assert.False(table.Methods.ContainsKey("hidden"));
    }

    // ── Metadata Tests ────────────────────────────────────

    [Fact]
    public void PureMetadata_IsPureTrue()
    {
        var table = new FFIHostTable();
        table.RegisterModule<MetadataModule>();

        var info = table.MethodInfos.First(m => m.Name == "documented");
        Assert.True(info.IsPure);
    }

    [Fact]
    public void DescriptionMetadata_Populated()
    {
        var table = new FFIHostTable();
        table.RegisterModule<MetadataModule>();

        var info = table.MethodInfos.First(m => m.Name == "documented");
        Assert.Equal("Adds r0 and r1, stores result in r0", info.Description);
    }

    [Fact]
    public void ParamMetadata_Populated()
    {
        var table = new FFIHostTable();
        table.RegisterModule<MetadataModule>();

        var info = table.MethodInfos.First(m => m.Name == "documented");
        Assert.NotNull(info.ParameterDescriptions);
        Assert.True(info.ParameterDescriptions!.ContainsKey("state"));
        Assert.Equal("First operand", info.ParameterDescriptions["state"]);
    }

    // ── Validation Tests ─────────────────────────────────

    [Fact]
    public void DuplicateName_ThrowsInvalidOperation()
    {
        var table = new FFIHostTable();
        table.RegisterModule(typeof(DuplicateNameModuleA));

        var ex = Assert.Throws<InvalidOperationException>(() =>
            table.RegisterModule(typeof(DuplicateNameModuleB))
        );
        Assert.Contains("conflict", ex.Message);
    }

    [Fact]
    public void DuplicateIndex_ThrowsInvalidOperation()
    {
        var table = new FFIHostTable();
        table.RegisterModule(typeof(DuplicateIndexModuleA));

        var ex = Assert.Throws<InvalidOperationException>(() =>
            table.RegisterModule(typeof(DuplicateIndexModuleB))
        );
        Assert.Contains("5", ex.Message);
    }

    [Fact]
    public void UnsupportedReturnType_ThrowsNotSupported()
    {
        var table = new FFIHostTable();
        var ex = Assert.Throws<NotSupportedException>(() =>
            table.RegisterModule<UnsupportedTypeModule>()
        );
        Assert.Contains("String", ex.Message);
    }

    [Fact]
    public void UnsupportedParamType_ThrowsNotSupported()
    {
        var table = new FFIHostTable();
        var ex = Assert.Throws<NotSupportedException>(() =>
            table.RegisterModule<UnsupportedParamModule>()
        );
        Assert.Contains("String", ex.Message);
    }

    // ── Assembly Scanning Test ────────────────────────────

    [Fact]
    public void FromAssembly_DiscoverAllModules()
    {
        var table = FFIHostTable.FromAssembly(typeof(FfiReflectionTests).Assembly);

        // Should contain methods from all [RaptorModule]-decorated types in this test assembly
        Assert.True(table.Methods.ContainsKey("doubleR0")); // StaticDirectBindModule
        Assert.True(table.Methods.ContainsKey("math.add")); // PrefixedModule
        Assert.True(table.Methods.ContainsKey("spawnEnemy")); // AutoNameModule
        Assert.True(table.Methods.ContainsKey("visible")); // IgnoreModule
        Assert.False(table.Methods.ContainsKey("hidden")); // RaptorIgnore
    }

    // ── CamelCase Conversion Tests ────────────────────────

    [Theory]
    [InlineData("SpawnEnemy", "spawnEnemy")]
    [InlineData("X", "x")]
    [InlineData("HTMLParser", "htmlParser")]
    [InlineData("already", "already")]
    [InlineData("A", "a")]
    [InlineData("AB", "ab")]
    [InlineData("ABC", "abc")]
    [InlineData("ABCDef", "abcDef")]
    public void ToCamelCase_ConvertsCorrectly(string input, string expected)
    {
        Assert.Equal(expected, FFIHostTable.ToCamelCase(input));
    }

    // ── End-to-End via ScriptEngine ───────────────────────

    [Fact]
    public void EndToEnd_RegisterHostTable_CompileAndRun()
    {
        var table = new FFIHostTable();
        table.RegisterModule<StaticDirectBindModule>();

        ScriptEngine engine = new ScriptEngine();
        engine.RegisterHostTable(table);

        VMChunk chunk = engine.Compile(
            @"
LOADC r1 100.0
CALL doubleR0() r1
MOVE r2 r1
HALT
"
        );
        ExecutionResult result = engine.Execute(chunk);
        Assert.Equal(VMStatus.Halted, result.Status);
        Assert.Equal(200.0, result.RegistersSnapshot[2]);
    }

    // ── Manual Registration Fallback ──────────────────────

    [Fact]
    public void ManualRegister_StillWorks()
    {
        var table = new FFIHostTable();
        table.Register(
            "tripleR0",
            10,
            (ref VMState state) =>
            {
                unsafe
                {
                    state.RegPtr[0] = state.RegPtr[0] * 3.0;
                }
            }
        );

        Assert.True(table.Methods.ContainsKey("tripleR0"));
        Assert.Equal((ushort)10, table.Methods["tripleR0"].Index);

        ScriptEngine engine = new ScriptEngine();
        engine.RegisterHostTable(table);
        ExecutionResult result = engine.Run(
            @"
LOADC r1 14.0
CALL tripleR0() r1
MOVE r2 r1
HALT
"
        );
        Assert.Equal(VMStatus.Halted, result.Status);
        Assert.Equal(42.0, result.RegistersSnapshot[2]);
    }

    [Fact]
    public void FallbackPath_RegistersAndExecutes()
    {
        var table = new FFIHostTable();
        table.RegisterModule<FallbackPathModule>();

        ScriptEngine engine = new ScriptEngine();
        engine.RegisterHostTable(table);
        ExecutionResult result = engine.Run(
            @"
LOADC r1 1.0
LOADC r2 2.0
LOADC r3 3.0
LOADC r4 4.0
LOADC r5 5.0
CALL sumFive() r1
MOVE r6 r1
HALT
"
        );
        Assert.Equal(VMStatus.Halted, result.Status);
        Assert.Equal(15.0, result.RegistersSnapshot[6]);
    }

    [Fact]
    public void GenerateAutocompleteDeclarations_OutputsValidTypeScript()
    {
        var table = new FFIHostTable();
        table.RegisterModule<TypingsTestModule>();

        string decls = table.GenerateAutocompleteDeclarations();
        Assert.Contains("\"myGame.spawn\":", decls);
        Assert.Contains("\"type\": \"method\"", decls);
        Assert.Contains("\"signature\": \"myGame.spawn(x, y)\"", decls);
        Assert.Contains("\"description\": \"Spawns an entity in the game world.\"", decls);
        Assert.Contains("\"name\": \"x\"", decls);
        Assert.Contains("\"description\": \"X coordinate\"", decls);
        Assert.Contains("\"name\": \"y\"", decls);
        Assert.Contains("\"description\": \"Y coordinate\"", decls);
    }
}

[RaptorModule("myGame")]
public class TypingsTestModule
{
    [RaptorMethod("spawn")]
    [RaptorDescription("Spawns an entity in the game world.")]
    public static double Spawn(
        [RaptorParam("X coordinate")] double x,
        [RaptorParam("Y coordinate")] double y
    )
    {
        return x + y;
    }
}
