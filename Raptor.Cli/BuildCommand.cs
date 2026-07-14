using System.ComponentModel;
using Raptor;
using Raptor.Compiler;
using Spectre.Console;
using Spectre.Console.Cli;

public class BuildCommand : Command<BuildCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [CommandArgument(0, "<FILE>")]
        public string ScriptPath { get; set; }

        [CommandOption("-a|--omit-assembly")]
        [Description("Omit raptor assembly if building.")]
        public bool OmitRaptorAssembly { get; set; }
    }

    private readonly ScriptEngine _engine;
    private readonly FFIHostTable _hostTable;

    public BuildCommand(FFIHostTable hostTable)
    {
        _hostTable = hostTable;
        _engine = new ScriptEngine();
        _engine.RegisterHostTable(_hostTable);
    }

    protected override int Execute(
        CommandContext context,
        BuildCommand.Settings settings,
        CancellationToken token
    )
    {
        string code = File.ReadAllText(settings.ScriptPath);
        string asm = RaptorScriptCompiler.Compile(code);
        if (!Path.Exists("build" + Path.DirectorySeparatorChar))
        {
            Directory.CreateDirectory("build");
        }
        string apiPath = Path.Combine(
            "build",
            Path.GetFileNameWithoutExtension(settings.ScriptPath) + "-api.json"
        );
        File.WriteAllText(apiPath, _hostTable.GenerateAutocompleteDeclarations());
        string targetPath = Path.Combine("build", settings.ScriptPath);
        if (settings.OmitRaptorAssembly)
            File.WriteAllText(Path.ChangeExtension(targetPath, "rasm"), asm);
        VMChunk compiledCode = _engine.Compile(asm);
        RaptorBinary.Save(compiledCode, Path.ChangeExtension(targetPath, "rbc"));
        return 0;
    }
}
