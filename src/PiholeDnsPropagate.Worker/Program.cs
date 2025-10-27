using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PiholeDnsPropagate.Worker;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<PropagationWorker>();

var host = builder.Build();
host.Run();
