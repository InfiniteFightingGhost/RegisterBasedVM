using System;
using System.ComponentModel;
using System.IO;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Raptor.Cli;

public class NewCommand : Command<NewCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [CommandArgument(0, "<FILE>")]
        [Description("The path or name of the new .rapt script file to create.")]
        public required string ScriptPath { get; set; }

        [CommandOption("-f|--force")]
        [Description("Overwrite the target file if it already exists.")]
        public bool Force { get; set; }
    }

    private const string DefaultTemplateContent = @"// template.rapt - RaptorScript Starter Template
// Fast, zero-allocation register-based scripting language for game engines.

// 1. Variable Declarations & Math Operations
var radius = 5.0;
var area = math.pi() * math.pow(radius, 2.0);

// 2. Output result via Host FFI call
peri.print(area);

// 3. Conditional Logic & Control Flow Loop
if (area > 50.0) {
    for (var i = 0; i < 5; i++) {
        var step = i * 10.0;
        peri.print(step);
    }
}
";

    public int ExecuteForTesting(Settings settings)
    {
        return Execute(null!, settings, CancellationToken.None);
    }

    protected override int Execute(
        CommandContext context,
        Settings settings,
        CancellationToken token
    )
    {
        string targetPath = settings.ScriptPath;
        if (!targetPath.EndsWith(".rapt", StringComparison.OrdinalIgnoreCase))
        {
            targetPath += ".rapt";
        }

        if (File.Exists(targetPath) && !settings.Force)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] File '{targetPath}' already exists. Use --force to overwrite.");
            return 1;
        }

        string? directory = Path.GetDirectoryName(targetPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(targetPath, DefaultTemplateContent);
        AnsiConsole.MarkupLine($"[green]Success:[/] Created new RaptorScript starter template at [bold]{targetPath}[/]");
        return 0;
    }
}
