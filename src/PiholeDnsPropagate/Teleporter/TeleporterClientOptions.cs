using System;

namespace PiholeDnsPropagate.Teleporter;

public sealed class TeleporterClientOptions
{
    public string InstanceName { get; set; } = string.Empty;
    public Uri BaseUrl { get; set; } = new("http://localhost");
    public string Password { get; set; } = string.Empty;
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);
}
