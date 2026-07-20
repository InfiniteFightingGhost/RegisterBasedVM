using System.ComponentModel;
using Raptor;
using Raptor.Compiler;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Raptor.Cli;

public class RunCommand : Command<RunCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [CommandArgument(0, "<FILE>")]
        [Description("The path of the script you wish to run.")]
        public required string ScriptPath { get; set; }

        [CommandOption("--no-build")]
        [Description("Run the raptor script without building it first.")]
        public bool RunWithoutBuilding { get; set; }

        [CommandOption("-a|--omit-assembly")]
        [Description("Omit raptor assembly if building.")]
        public bool OmitRaptorAssembly { get; set; }
    }

    private readonly ScriptEngine _engine = new();
    private readonly FFIHostTable _hostTable;

    public RunCommand(FFIHostTable table)
    {
        _hostTable = table;
        _engine.RegisterHostTable(_hostTable);
    }

    public int ExecuteForTesting(Settings settings)
    {
        return Execute(null!, settings, CancellationToken.None);
    }

    protected override int Execute(
        CommandContext context,
        RunCommand.Settings settings,
        CancellationToken token
    )
    {
        if (!Path.Exists(settings.ScriptPath))
        {
            AnsiConsole.MarkupLine(
                $"[red bold]File \"{Markup.Escape(settings.ScriptPath)}\" not found[/]"
            );
            return 1;
        }
        if (!settings.RunWithoutBuilding)
        {
            string code = File.ReadAllText(settings.ScriptPath);
            var reporter = new DiagnosticReporter();
            string asm = string.Empty;
            try
            {
                asm = RaptorScriptCompiler.Compile(code, reporter: reporter);
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
            var result = _engine.Execute(compiledCode);
            if (result.ErrorMessage != null)
            {
                AnsiConsole.MarkupLine("[red bold]Error occured at raptor script[/]");
                AnsiConsole.MarkupLine($"[red bold]{Markup.Escape(result.ErrorMessage)}[/]");
                AnsiConsole.MarkupLine(
                    $"[red bold]{Markup.Escape(ScriptEngine.TranslateError(compiledCode, result.IpOffset, code))}[/]"
                );
                return 1;
            }
        }
        else
        {
            string buildPath = Path.Combine("build", Path.GetFileName(settings.ScriptPath));
            buildPath = Path.ChangeExtension(buildPath, "rbc");
            if (!Path.Exists(buildPath))
            {
                AnsiConsole.MarkupLine($"[red bold]Build file not found.[/]");
                AnsiConsole.MarkupLine(
                    $"[red bold]Make sure to build first or use 'raptor run' without --no-building.[/]"
                );
                return 1;
            }
            ExecutionResult result = new ExecutionResult();
            try
            {
                result = _engine.Execute(buildPath);
                AnsiConsole.WriteLine((_engine == null).ToString());
            }
            catch (NullReferenceException ex)
            {
                AnsiConsole.MarkupLine($"[red bold]{Markup.Escape(ex.Message)}[/]");
                AnsiConsole.MarkupLine(
                    $"[red bold]{Markup.Escape(ex.StackTrace ?? string.Empty)}[/]"
                );
            }
            if (result.ErrorMessage != null)
            {
                AnsiConsole.MarkupLine("[red bold]Error occured at raptor script[/]");
                AnsiConsole.MarkupLine($"[red bold]{Markup.Escape(result.ErrorMessage)}[/]");
                return 1;
            }
        }
        return 0;
    }
}
