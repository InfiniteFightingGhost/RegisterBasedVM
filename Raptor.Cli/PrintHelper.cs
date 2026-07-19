using System.Text;
using Raptor.Compiler;
using Spectre.Console;

namespace Raptor.Cli
{
    public static class PrintHelper
    {
        public static void PrintDiagnostic(Diagnostic diagnostic, string fileName, string[] code)
        {
            if (diagnostic.Severity == DiagnosticSeverity.Error)
            {
                AnsiConsole.Markup($"[red]{diagnostic.Severity}[/]");
                AnsiConsole.Markup($"[red][[{Markup.Escape(diagnostic.Code)}]] : [/]");
            }
            else // Warning
            {
                AnsiConsole.Markup($"[yellow]{diagnostic.Severity}[/]");
                AnsiConsole.Markup($"[yellow][[{Markup.Escape(diagnostic.Code)}]] : [/]");
            }
            AnsiConsole.MarkupLine($"[white bold]{Markup.Escape(diagnostic.Message)}[/]");
            AnsiConsole.Markup($"[blue]  --> [/]");
            AnsiConsole.MarkupLine(
                $"[white]{Markup.Escape(fileName)}:{diagnostic.Line}:{diagnostic.Column}[/]"
            );
            string lineStr = diagnostic.Line.ToString();
            string pad = new string(' ', lineStr.Length);
            AnsiConsole.MarkupLine($"[blue] {pad} |[/]");
            string line = code[diagnostic.Line - 1];
            AnsiConsole.Markup($"[blue] {lineStr} |  [/]");
            AnsiConsole.MarkupLine($"[white]{Markup.Escape(line)}[/]");

            // Match padding spaces/tabs to line tabs
            StringBuilder padding = new StringBuilder();
            for (int i = 0; i < Math.Min(diagnostic.Column - 1, line.Length); i++)
            {
                padding.Append(line[i] == '\t' ? '\t' : ' ');
            }

            string underCaret = new string('^', diagnostic.Length);
            AnsiConsole.Markup($"[blue] {pad} |  [/]");
            string annotation =
                diagnostic.Annotation != null ? Markup.Escape(diagnostic.Annotation) : string.Empty;
            AnsiConsole.MarkupLine($"[red]{padding}{underCaret}[/][white]{annotation}[/]");
        }
    }
}
