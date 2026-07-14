using Microsoft.Extensions.DependencyInjection;
using Raptor;
using Raptor.Cli;
using Raptor.StdLib;
using Spectre.Console.Cli;

var table = new FFIHostTable();
table.RegisterModule(typeof(RaptorMath));
table.RegisterModule(typeof(RaptorPeriferals));

var services = new ServiceCollection();
services.AddSingleton(table);

var registrar = new TypeRegistrar(services);
var app = new CommandApp(registrar);

app.Configure(config =>
{
    config.AddCommand<RunCommand>("run");
    config.AddCommand<DocsCommand>("docs");
    config.AddCommand<BuildCommand>("build");
});

return app.Run(args);
