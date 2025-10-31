using System;
using System.Collections.Generic;
using System.Linq;

namespace PiholeDnsPropagate.Teleporter;

public sealed record TeleporterDnsRecords(
    IReadOnlyList<string> Hosts,
    IReadOnlyList<string> CnameRecords
)
{
    public static readonly TeleporterDnsRecords Empty = new(Array.Empty<string>(), Array.Empty<string>());

    public bool ContentEquals(TeleporterDnsRecords? other)
    {
        if (other is null)
        {
            return false;
        }

        return SequenceEquals(Hosts, other.Hosts)
            && SequenceEquals(CnameRecords, other.CnameRecords);
    }

    private static bool SequenceEquals(IReadOnlyList<string> left, IReadOnlyList<string> right)
    {
        if (left.Count != right.Count)
        {
            return false;
        }

        var comparer = StringComparer.OrdinalIgnoreCase;
        var leftOrdered = left.OrderBy(static value => value, comparer);
        var rightOrdered = right.OrderBy(static value => value, comparer);
        return leftOrdered.SequenceEqual(rightOrdered, comparer);
    }
}
