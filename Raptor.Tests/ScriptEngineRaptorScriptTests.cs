using System;
using System.IO;
using Raptor;
using Raptor.StdLib;
using Xunit;

namespace Raptor.Tests
{
    public class ScriptEngineRaptorScriptTests
    {
        [Fact]
        public void ScriptEngineCompileRaptorScriptTest()
        {
            using var engine = new ScriptEngine();
            var table = new FFIHostTable();
            table.RegisterModule(typeof(RaptorMath));
            engine.RegisterHostTable(table);

            string raptorScript = @"
                var radius = 5.0;
                var area = math.pi() * math.pow(radius, 2.0);
            ";

            VMChunk chunk = engine.CompileRaptorScript(raptorScript);
            Assert.NotNull(chunk);
            Assert.NotEmpty(chunk.Instructions);

            ExecutionResult result = engine.Execute(chunk);
            Assert.Equal(VMStatus.Halted, result.Status);
        }

        [Fact]
        public void ScriptEngineRunRaptorScriptTest()
        {
            using var engine = new ScriptEngine();
            string raptorScript = @"
                var x = 10;
                var y = 20;
                var z = x + y;
            ";

            ExecutionResult result = engine.RunRaptorScript(raptorScript);
            Assert.Equal(VMStatus.Halted, result.Status);
        }

        [Fact]
        public void ScriptEngineSmartCompileAutoDetectsRaptorScriptAndAssembly()
        {
            using var engine = new ScriptEngine();

            // High-level RaptorScript auto-detection via engine.Compile
            string raptorScript = "var x = 42;";
            VMChunk scriptChunk = engine.Compile(raptorScript);
            Assert.NotNull(scriptChunk);
            ExecutionResult scriptResult = engine.Execute(scriptChunk);
            Assert.Equal(VMStatus.Halted, scriptResult.Status);

            // Assembly auto-detection via engine.Compile
            string rasmScript = @"
                LOADC r1 42.0
                HALT
            ";
            VMChunk rasmChunk = engine.Compile(rasmScript);
            Assert.NotNull(rasmChunk);
            ExecutionResult rasmResult = engine.Execute(rasmChunk);
            Assert.Equal(VMStatus.Halted, rasmResult.Status);
            Assert.Equal(42.0, rasmResult.RegistersSnapshot[1]);
        }
    }
}
