using System.ComponentModel;
using Raptor.Compiler;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Raptor.Cli;

public class RunCommand : Command<RunCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [CommandArgument(0, "<FILE>")]
        public string ScriptPath { get; set; }

        [CommandOption("--no-build")]
        [Description("Run the raptor script without building it first.")]
        public bool RunWithoutBuilding { get; set; }

        [CommandOption("-a|--omit-assembly")]
        [Description("Omit raptor assembly if building.")]
        public bool OmitRaptorAssembly { get; set; }
    }

    private readonly ScriptEngine _engine;

    public RunCommand(ScriptEngine engine)
    {
        _engine = engine;
    }

    protected override int Execute(
        CommandContext context,
        RunCommand.Settings settings,
        CancellationToken token
    )
    {
        if (!Path.Exists(settings.ScriptPath))
        {
            AnsiConsole.WriteLine($"[red bold]File \"{settings.ScriptPath}\" not found[/]");
            return 1;
        }
        if (!settings.RunWithoutBuilding)
        {
            string code = File.ReadAllText(settings.ScriptPath);
            string asm = RaptorScriptCompiler.Compile(code);
            if (!Path.Exists("build" + Path.DirectorySeparatorChar))
            {
                Directory.CreateDirectory("build");
            }
            string targetPath = Path.Combine("build", settings.ScriptPath);
            if (settings.OmitRaptorAssembly)
                File.WriteAllText(Path.ChangeExtension(targetPath, "rasm"), asm);
            VMChunk compiledCode = _engine.Compile(asm);
            RaptorBinary.Save(compiledCode, Path.ChangeExtension(targetPath, "rbc"));
            var result = _engine.Execute(compiledCode);
            if (result.ErrorMessage != null)
            {
                AnsiConsole.WriteLine("[red bold]Error occured at raptor script[/]");
                AnsiConsole.WriteLine($"[red bold]{result.ErrorMessage}[/]");
                AnsiConsole.WriteLine(
                    $"[red bold]{ScriptEngine.TranslateError(compiledCode, result.IpOffset, code)}[/]"
                );
                return 1;
            }
            return 0;
        }
        else
        {
            string buildPath = Path.Combine("build", settings.ScriptPath);
            buildPath = Path.ChangeExtension(buildPath, "rbc");
            if (!Path.Exists(buildPath))
            {
                AnsiConsole.WriteLine($"[red bold]Build file not found.[/]");
                AnsiConsole.WriteLine(
                    $"[red bold]Make sure to build first or use raptor run without --no-building.[/]"
                );
            }
        }
        return 0;
    }
}
