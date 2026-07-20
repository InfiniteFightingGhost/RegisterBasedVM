using System.ComponentModel;
using Raptor;
using Raptor.Compiler;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Raptor.Cli
{
    public class BuildCommand : Command<BuildCommand.Settings>
    {
        public class Settings : CommandSettings
        {
            [CommandArgument(0, "<FILE>")]
            public required string ScriptPath { get; set; }

            [CommandOption("-a|--omit-assembly")]
            [Description("Omit raptor assembly.")]
            public bool OmitRaptorAssembly { get; set; }

            [CommandOption("-p|--print-ast")]
            [Description("Print the compiled abstract syntax tree")]
            public bool PrintAst { get; set; }
        }

        private readonly ScriptEngine _engine;
        private readonly FFIHostTable _hostTable;

        public BuildCommand(FFIHostTable hostTable)
        {
            _hostTable = hostTable;
            _engine = new ScriptEngine();
            _engine.RegisterHostTable(_hostTable);
        }

        public int ExecuteForTesting(Settings settings)
        {
            return Execute(null!, settings, CancellationToken.None);
        }

        protected override int Execute(
            CommandContext context,
            BuildCommand.Settings settings,
            CancellationToken token
        )
        {
            string code = File.ReadAllText(settings.ScriptPath);
            DiagnosticReporter reporter = new DiagnosticReporter();
            string asm = string.Empty;
            try
            {
                asm = RaptorScriptCompiler.Compile(
                    code,
                    printAst: settings.PrintAst,
                    reporter: reporter
                );
            }
            catch (CompileException) { }
            finally
            {
                if (reporter.HasErrors)
                {
                    string[] codeLines = code.Split('\n').ToArray();
                    foreach (var error in reporter.Diagnostics)
                    {
                        PrintHelper.PrintDiagnostic(error, settings.ScriptPath, codeLines);
                    }
                }
            }
            if (reporter.HasErrors)
                return 1;
            if (!Path.Exists("build" + Path.DirectorySeparatorChar))
            {
                Directory.CreateDirectory("build");
            }
            string apiPath = Path.Combine(
                "build",
                Path.GetFileNameWithoutExtension(settings.ScriptPath) + "-api.json"
            );
            File.WriteAllText(apiPath, _hostTable.GenerateAutocompleteDeclarations());
            string targetPath = Path.Combine("build", Path.GetFileName(settings.ScriptPath));
            if (settings.OmitRaptorAssembly)
                File.WriteAllText(Path.ChangeExtension(targetPath, "rasm"), asm);
            VMChunk compiledCode = _engine.Compile(asm);
            RaptorBinary.Save(compiledCode, Path.ChangeExtension(targetPath, "rbc"));
            return 0;
        }
    }
}
