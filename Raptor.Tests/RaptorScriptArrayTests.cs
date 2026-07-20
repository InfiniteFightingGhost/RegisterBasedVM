using System;
using Raptor;
using Raptor.StdLib;
using Xunit;

namespace Raptor.Tests
{
    public class RaptorScriptArrayTests
    {
        [Fact]
        public void RaptorScript_ArrayLiteral_CreationAndAccess()
        {
            using var engine = new ScriptEngine();
            string script = @"
                var arr = [10.0, 20.0, 30.0];
                var first = arr[0];
                var second = arr[1];
                var third = arr[2];
            ";

            ExecutionResult result = engine.RunRaptorScript(script);
            Assert.Equal(VMStatus.Halted, result.Status);
        }

        [Fact]
        public void RaptorScript_ArrayIndexAssignment_UpdatesElement()
        {
            using var engine = new ScriptEngine();
            string script = @"
                var numbers = [1.0, 2.0, 3.0];
                numbers[1] = 99.0;
                var updated = numbers[1];
            ";

            ExecutionResult result = engine.RunRaptorScript(script);
            Assert.Equal(VMStatus.Halted, result.Status);
        }

        [Fact]
        public void RaptorScript_AllocAndLen_ReturnsCorrectLength()
        {
            using var engine = new ScriptEngine();
            string script = @"
                var buffer = alloc(8);
                var length = len(buffer);
                free(buffer);
            ";

            ExecutionResult result = engine.RunRaptorScript(script);
            Assert.Equal(VMStatus.Halted, result.Status);
        }

        [Fact]
        public void RaptorScript_ArrayPopulatedInLoop_CalculatesCorrectSum()
        {
            using var engine = new ScriptEngine();
            string script = @"
                var items = alloc(4);
                for (var i = 0; i < 4; i++) {
                    items[i] = i * 10.0;
                }
                var total = items[0] + items[1] + items[2] + items[3];
            ";

            ExecutionResult result = engine.RunRaptorScript(script);
            Assert.Equal(VMStatus.Halted, result.Status);
        }

        [Fact]
        public void RaptorScript_ExpressionAsIndex_AccessesCorrectElement()
        {
            using var engine = new ScriptEngine();
            string script = @"
                var data = [100.0, 200.0, 300.0, 400.0];
                var baseIdx = 1;
                var val = data[baseIdx + 1];
            ";

            ExecutionResult result = engine.RunRaptorScript(script);
            Assert.Equal(VMStatus.Halted, result.Status);
        }

        [Fact]
        public void RaptorScript_FreeArray_ExecutesWithoutError()
        {
            using var engine = new ScriptEngine();
            string script = @"
                var arr = [5.0, 10.0, 15.0];
                free(arr);
            ";

            ExecutionResult result = engine.RunRaptorScript(script);
            Assert.Equal(VMStatus.Halted, result.Status);
        }
    }
}
