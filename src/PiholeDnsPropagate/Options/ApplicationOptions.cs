namespace PiholeDnsPropagate.Options;

public sealed class ApplicationOptions
{
    public string LogLevel { get; set; } = "Information";
    public int HealthPort { get; set; } = 8080;
}
