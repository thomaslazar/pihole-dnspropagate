using System;

namespace PiholeDnsPropagate.Options;

public sealed class PrimaryPiHoleOptions
{
    public Uri? BaseUrl { get; set; }

    /// <summary>
    /// Plaintext admin password provided via environment variables.
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// SHA256 hash derived from <see cref="Password"/> during application start.
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;
}
