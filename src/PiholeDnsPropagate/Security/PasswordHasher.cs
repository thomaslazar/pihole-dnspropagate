using System;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;

namespace PiholeDnsPropagate.Security;

internal static class PasswordHasher
{
    [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase",
        Justification = "Pi-hole expects lowercase hexadecimal hashes.")]
    public static string ComputeSha256(string value)
    {
        var normalized = value ?? string.Empty;
        var bytes = Encoding.UTF8.GetBytes(normalized);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
