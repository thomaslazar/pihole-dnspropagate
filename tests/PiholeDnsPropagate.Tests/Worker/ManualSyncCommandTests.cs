using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NUnit.Framework;
using PiholeDnsPropagate.Teleporter;
using PiholeDnsPropagate.Worker.Services;
using PiholeDnsPropagate.Worker.Scheduling;

namespace PiholeDnsPropagate.Tests.Worker;

[TestFixture]
[SuppressMessage("MicrosoftDesign", "CA1515", Justification = "NUnit requires public fixtures for discovery.")]
public sealed class ManualSyncCommandTests
{
    [Test]
    public async Task ExecuteAsyncRunsCoordinatorWhenIdle()
    {
        // Arrange
        var coordinator = Substitute.For<ISyncCoordinator>();
        var syncState = Substitute.For<ISyncState>();
        syncState.TryMarkRunning().Returns(true);
        coordinator.SynchronizeAsync(false, default).Returns(new SyncResult(
            new SyncNodeResult("primary", SyncStatus.Success, new RecordCounts(1, 1)),
            Array.Empty<SyncNodeResult>()));
        var command = new ManualSyncCommand(coordinator, syncState, NullLogger<ManualSyncCommand>.Instance);
        var settings = new ManualSyncCommandSettings { DryRun = false };

        // Act
        var exitCode = await command.ExecuteAsync(null, settings, CancellationToken.None).ConfigureAwait(false);

        // Assert
        Assert.That(exitCode, Is.EqualTo(0));
        await coordinator.Received(1).SynchronizeAsync(false, Arg.Any<CancellationToken>()).ConfigureAwait(false);
        syncState.Received().MarkSuccess(Arg.Any<DateTimeOffset>());
    }

    [Test]
    public async Task ExecuteAsyncRejectsWhenAlreadyRunning()
    {
        // Arrange
        var coordinator = Substitute.For<ISyncCoordinator>();
        var syncState = Substitute.For<ISyncState>();
        syncState.TryMarkRunning().Returns(false);
        var command = new ManualSyncCommand(coordinator, syncState, NullLogger<ManualSyncCommand>.Instance);

        // Act
        var exitCode = await command.ExecuteAsync(null, new ManualSyncCommandSettings(), CancellationToken.None).ConfigureAwait(false);

        // Assert
        Assert.That(exitCode, Is.EqualTo(-1));
        await coordinator.DidNotReceiveWithAnyArgs().SynchronizeAsync(default, default).ConfigureAwait(false);
    }
}
