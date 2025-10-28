using System;

namespace PiholeDnsPropagate.Teleporter.Authentication;

internal sealed class PiHoleSession
{
    public string Sid { get; init; } = string.Empty;
    public string? CsrfToken { get; init; }
    public bool TotpEnabled { get; init; }
    public TimeSpan Validity { get; init; } = TimeSpan.FromMinutes(5);
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    public DateTimeOffset ExpiresAt => CreatedAt + Validity;

    public bool IsExpired(DateTimeOffset timestamp) => timestamp >= ExpiresAt;
}
