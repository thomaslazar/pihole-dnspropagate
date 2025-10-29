using System.Collections.Generic;

namespace PiholeDnsPropagate.Teleporter;

public sealed record SyncResult(
    SyncNodeResult Primary,
    IReadOnlyList<SyncNodeResult> Secondaries
)
{
    public static readonly SyncResult Empty = new(new SyncNodeResult("primary", SyncStatus.Skipped), new List<SyncNodeResult>());
}

public sealed record SyncNodeResult(
    string NodeName,
    SyncStatus Status,
    RecordCounts? Before = null,
    RecordCounts? After = null,
    string? Error = null
);

public sealed record RecordCounts(int Hosts, int Cnames);

public enum SyncStatus
{
    Success,
    Failed,
    Skipped
}
