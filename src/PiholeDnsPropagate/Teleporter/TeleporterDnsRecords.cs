using System;
using System.Collections.Generic;

namespace PiholeDnsPropagate.Teleporter;

public sealed record TeleporterDnsRecords(
    IReadOnlyList<string> Hosts,
    IReadOnlyList<string> CnameRecords
)
{
    public static readonly TeleporterDnsRecords Empty = new(Array.Empty<string>(), Array.Empty<string>());
}
