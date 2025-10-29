using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PiholeDnsPropagate.Extensions;
using PiholeDnsPropagate.Worker.Cli;
using PiholeDnsPropagate.Worker.Scheduling;
using PiholeDnsPropagate.Worker.Services;
using Spectre.Console.Cli;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddApplicationOptions(builder.Configuration);
builder.Services.AddSingleton<ISyncState, SyncState>();
builder.Services.AddHostedService<CronSchedulerService>();
builder.Services.AddHostedService<HealthEndpointService>();
builder.Services.AddTransient<ManualSyncCommand>();

if (args.Length > 0)
{
    var registrar = new TypeRegistrar(builder.Services);
    var app = new CommandApp(registrar);
    app.Configure(config =>
    {
        config.AddCommand<ManualSyncCommand>("sync-now")
            .WithDescription("Run an immediate synchronization against configured Pi-hole instances.");
    });

    return await app.RunAsync(args).ConfigureAwait(false);
}

var host = builder.Build();
await host.RunAsync().ConfigureAwait(false);

return 0;
