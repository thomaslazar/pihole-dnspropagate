using System;
using System.Diagnostics.CodeAnalysis;

namespace PiholeDnsPropagate.Worker.Services;

[SuppressMessage("MicrosoftDesign", "CA1515", Justification = "Shared with CLI command and test assembly via DI.")]
public interface ISyncState
{
    DateTimeOffset? LastSuccess { get; }
    DateTimeOffset? LastFailure { get; }
    SyncRunStatus CurrentStatus { get; }
    bool IsRunning { get; }

    bool TryMarkRunning();
    void MarkSuccess(DateTimeOffset timestamp);
    void MarkFailure(DateTimeOffset timestamp);
    void MarkIdle();
}

[SuppressMessage("MicrosoftDesign", "CA1515", Justification = "Enum accompanies ISyncState for health reporting.")]
public enum SyncRunStatus
{
    Idle,
    Running,
    Success,
    Failure
}
