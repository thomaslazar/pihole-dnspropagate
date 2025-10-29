using System;
using System.Diagnostics.CodeAnalysis;
using NUnit.Framework;
using PiholeDnsPropagate.Worker.Services;

namespace PiholeDnsPropagate.Tests.Worker;

[TestFixture]
[SuppressMessage("MicrosoftDesign", "CA1515", Justification = "NUnit requires public fixtures for discovery.")]
public sealed class SyncStateTests
{
    [Test]
    public void TryMarkRunningPreventsOverlap()
    {
        // Arrange
        var state = new SyncState();

        // Act
        var first = state.TryMarkRunning();
        var second = state.TryMarkRunning();

        // Assert
        Assert.That(first, Is.True);
        Assert.That(second, Is.False);
        state.MarkIdle();
        Assert.That(state.TryMarkRunning(), Is.True);
    }

    [Test]
    public void MarkSuccessUpdatesTimestampsAndStatus()
    {
        // Arrange
        var state = new SyncState();
        state.TryMarkRunning();
        var timestamp = DateTimeOffset.UtcNow;

        // Act
        state.MarkSuccess(timestamp);

        // Assert
        Assert.That(state.LastSuccess, Is.EqualTo(timestamp));
        Assert.That(state.CurrentStatus, Is.EqualTo(SyncRunStatus.Success));
        Assert.That(state.IsRunning, Is.False);
    }

    [Test]
    public void MarkFailureUpdatesTimestampsAndStatus()
    {
        // Arrange
        var state = new SyncState();
        state.TryMarkRunning();
        var timestamp = DateTimeOffset.UtcNow;

        // Act
        state.MarkFailure(timestamp);

        // Assert
        Assert.That(state.LastFailure, Is.EqualTo(timestamp));
        Assert.That(state.CurrentStatus, Is.EqualTo(SyncRunStatus.Failure));
        Assert.That(state.IsRunning, Is.False);
    }
}
