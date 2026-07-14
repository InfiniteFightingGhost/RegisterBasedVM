using Microsoft.Extensions.DependencyInjection;
using Raptor;
using Raptor.Cli;
using Raptor.StdLib;
using Spectre.Console.Cli;

var table = new FFIHostTable();
table.RegisterModule(typeof(RaptorMath));
table.RegisterModule(typeof(RaptorPeriferals));
var engine = new ScriptEngine();
engine.RegisterHostTable(table);

var services = new ServiceCollection();
services.AddSingleton(engine);

var registrar = new TypeRegistrar(services);
var app = new CommandApp(registrar);

app.Configure(config =>
{
    config.AddCommand<RunCommand>("run");
});

return app.Run(args);
