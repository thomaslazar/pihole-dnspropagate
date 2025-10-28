using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PiholeDnsPropagate.Extensions;
using PiholeDnsPropagate.Worker;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<PropagationWorker>();
builder.Services.AddApplicationOptions(builder.Configuration);

var host = builder.Build();
host.Run();
