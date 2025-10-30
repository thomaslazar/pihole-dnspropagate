using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using PiholeDnsPropagate.Options;
using NSubstitute;
using NUnit.Framework;
using PiholeDnsPropagate.Teleporter;
using PiholeDnsPropagate.Worker.Services;
using PiholeDnsPropagate.Worker.Scheduling;
using PiholeDnsPropagate.Tests.Teleporter.Fixtures;

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
        var command = new ManualSyncCommand(
            coordinator,
            syncState,
            new TestOptionsMonitor<SynchronizationOptions>(new SynchronizationOptions { DryRun = false }),
            NullLogger<ManualSyncCommand>.Instance);
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
        var command = new ManualSyncCommand(
            coordinator,
            syncState,
            new TestOptionsMonitor<SynchronizationOptions>(new SynchronizationOptions()),
            NullLogger<ManualSyncCommand>.Instance);

        // Act
        var exitCode = await command.ExecuteAsync(null, new ManualSyncCommandSettings(), CancellationToken.None).ConfigureAwait(false);

        // Assert
        Assert.That(exitCode, Is.EqualTo(-1));
        await coordinator.DidNotReceiveWithAnyArgs().SynchronizeAsync(default, default).ConfigureAwait(false);
    }

    [Test]
    public async Task ExecuteAsyncReturnsFailureCodeOnException()
    {
        // Arrange
        var coordinator = Substitute.For<ISyncCoordinator>();
        var syncState = Substitute.For<ISyncState>();
        syncState.TryMarkRunning().Returns(true);
        coordinator
            .SynchronizeAsync(false, Arg.Any<CancellationToken>())
            .Returns<Task<SyncResult>>(_ => throw new InvalidOperationException("boom"));
        var command = new ManualSyncCommand(
            coordinator,
            syncState,
            new TestOptionsMonitor<SynchronizationOptions>(new SynchronizationOptions()),
            NullLogger<ManualSyncCommand>.Instance);

        // Act
        var exitCode = await command.ExecuteAsync(null, new ManualSyncCommandSettings(), CancellationToken.None).ConfigureAwait(false);

        // Assert
        Assert.That(exitCode, Is.EqualTo(1));
        syncState.Received().MarkFailure(Arg.Any<DateTimeOffset>());
        syncState.Received().MarkIdle();
    }

    [Test]
    public async Task ExecuteAsyncReturnsPartialFailureWhenSecondaryFails()
    {
        // Arrange
        var coordinator = Substitute.For<ISyncCoordinator>();
        var syncState = Substitute.For<ISyncState>();
        syncState.TryMarkRunning().Returns(true);
        var syncResult = new SyncResult(
            new SyncNodeResult("primary", SyncStatus.Success, new RecordCounts(1, 1)),
            new[]
            {
                new SyncNodeResult("secondary", SyncStatus.Failed, new RecordCounts(1, 1), Error: "failure")
            });
        coordinator.SynchronizeAsync(false, Arg.Any<CancellationToken>()).Returns(Task.FromResult(syncResult));
        var command = new ManualSyncCommand(
            coordinator,
            syncState,
            new TestOptionsMonitor<SynchronizationOptions>(new SynchronizationOptions()),
            NullLogger<ManualSyncCommand>.Instance);

        // Act
        var exitCode = await command.ExecuteAsync(null, new ManualSyncCommandSettings(), CancellationToken.None).ConfigureAwait(false);

        // Assert
        Assert.That(exitCode, Is.EqualTo(2));
        syncState.Received().MarkFailure(Arg.Any<DateTimeOffset>());
        syncState.Received().MarkIdle();
    }

    [Test]
    public async Task ExecuteAsyncDefaultsToConfigurationDryRun()
    {
        // Arrange
        var coordinator = Substitute.For<ISyncCoordinator>();
        var syncState = Substitute.For<ISyncState>();
        syncState.TryMarkRunning().Returns(true);
        coordinator.SynchronizeAsync(true, Arg.Any<CancellationToken>()).Returns(new SyncResult(
            new SyncNodeResult("primary", SyncStatus.Success, new RecordCounts(1, 1)),
            Array.Empty<SyncNodeResult>()));

        var command = new ManualSyncCommand(
            coordinator,
            syncState,
            new TestOptionsMonitor<SynchronizationOptions>(new SynchronizationOptions { DryRun = true }),
            NullLogger<ManualSyncCommand>.Instance);

        // Act
        var exitCode = await command.ExecuteAsync(null, new ManualSyncCommandSettings(), CancellationToken.None).ConfigureAwait(false);

        // Assert
        Assert.That(exitCode, Is.EqualTo(0));
        await coordinator.Received(1).SynchronizeAsync(true, Arg.Any<CancellationToken>()).ConfigureAwait(false);
    }
}
