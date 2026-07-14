using System.Diagnostics;
using Spectre.Console;
using Spectre.Console.Cli;

public class DocsCommand : Command<DocsCommand.Settings>
{
    public class Settings : CommandSettings { }

    protected override int Execute(
        CommandContext context,
        DocsCommand.Settings settings,
        CancellationToken token
    )
    {
        var psi = new ProcessStartInfo
        {
            FileName = "https://github.com/InfiniteFightingGhost/Raptor/tree/main/docs",
            UseShellExecute = true,
        };

        Process.Start(psi);
        return 0;
    }
}
