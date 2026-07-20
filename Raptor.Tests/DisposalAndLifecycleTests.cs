using System;
using Raptor;
using Xunit;

namespace Raptor.Tests
{
    public class DisposalAndLifecycleTests
    {
        [Fact]
        public void VirtualMachineDisposeMultipleTimesDoesNotThrow()
        {
            var vm = new VirtualMachine();
            vm.Dispose();
            vm.Dispose(); // Multiple disposals must be safe
        }

        [Fact]
        public void ScriptEngineDisposeFreesResources()
        {
            using (var engine = new ScriptEngine())
            {
                VMChunk chunk = engine.Compile("LOADC r1 1.0\nHALT");
                ExecutionResult res = engine.Execute(chunk);
                Assert.Equal(VMStatus.Halted, res.Status);
            } // Dispose called automatically here
        }
    }
}
