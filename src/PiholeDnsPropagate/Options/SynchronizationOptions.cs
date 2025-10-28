using System;

namespace PiholeDnsPropagate.Options;

public sealed class SynchronizationOptions
{
    public TimeSpan Interval { get; set; } = TimeSpan.FromMinutes(5);
    public string? CronExpression { get; set; }
    public bool DryRun { get; set; }
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);
}
