using System;
using System.IO;
using Raptor.Cli;
using Raptor.StdLib;
using Spectre.Console.Cli;
using Xunit;

namespace Raptor.Tests
{
    public class CliCommandTests
    {
        [Fact]
        public void BuildCommandCompilesScriptFile()
        {
            string tempScript = Path.Combine(Path.GetTempPath(), "cli_test_script.rapt");
            File.WriteAllText(tempScript, "var x = 100;\nperi.print(x);");
            try
            {
                var table = new FFIHostTable();
                table.RegisterModule(typeof(RaptorMath));
                table.RegisterModule(typeof(RaptorPeripherals));

                var cmd = new BuildCommand(table);
                var settings = new BuildCommand.Settings { ScriptPath = tempScript };
                int exitCode = cmd.ExecuteForTesting(settings);

                Assert.Equal(0, exitCode);
                string rbcPath = Path.Combine("build", Path.GetFileNameWithoutExtension(tempScript) + ".rbc");
                Assert.True(File.Exists(rbcPath));
            }
            finally
            {
                if (File.Exists(tempScript))
                    File.Delete(tempScript);
            }
        }

        [Fact]
        public void RunCommandExecutesScriptFile()
        {
            string tempScript = Path.Combine(Path.GetTempPath(), "cli_run_script.rapt");
            File.WriteAllText(tempScript, "var a = 5;\nvar b = 15;\nvar c = a + b;");
            try
            {
                var table = new FFIHostTable();
                table.RegisterModule(typeof(RaptorMath));
                table.RegisterModule(typeof(RaptorPeripherals));

                var cmd = new RunCommand(table);
                var settings = new RunCommand.Settings { ScriptPath = tempScript };
                int exitCode = cmd.ExecuteForTesting(settings);

                Assert.Equal(0, exitCode);
            }
            finally
            {
                if (File.Exists(tempScript))
                    File.Delete(tempScript);
            }
        }
    }
}
