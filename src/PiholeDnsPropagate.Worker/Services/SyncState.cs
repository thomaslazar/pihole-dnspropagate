using System;
using System.Threading;

namespace PiholeDnsPropagate.Worker.Services;

internal sealed class SyncState : ISyncState
{
    private int _running;

    public DateTimeOffset? LastSuccess { get; private set; }
    public DateTimeOffset? LastFailure { get; private set; }
    public SyncRunStatus CurrentStatus { get; private set; } = SyncRunStatus.Idle;
    public bool IsRunning => Interlocked.CompareExchange(ref _running, 0, 0) == 1;

    public bool TryMarkRunning()
    {
        if (Interlocked.CompareExchange(ref _running, 1, 0) != 0)
        {
            return false;
        }

        CurrentStatus = SyncRunStatus.Running;
        return true;
    }

    public void MarkSuccess(DateTimeOffset timestamp)
    {
        LastSuccess = timestamp;
        CurrentStatus = SyncRunStatus.Success;
        MarkIdle();
    }

    public void MarkFailure(DateTimeOffset timestamp)
    {
        LastFailure = timestamp;
        CurrentStatus = SyncRunStatus.Failure;
        MarkIdle();
    }

    public void MarkIdle()
    {
        Interlocked.Exchange(ref _running, 0);
    }
}
