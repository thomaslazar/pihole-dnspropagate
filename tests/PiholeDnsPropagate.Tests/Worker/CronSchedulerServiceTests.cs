using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NUnit.Framework;
using PiholeDnsPropagate.Options;
using PiholeDnsPropagate.Teleporter;
using PiholeDnsPropagate.Tests.Teleporter.Fixtures;
using PiholeDnsPropagate.Worker.Scheduling;
using PiholeDnsPropagate.Worker.Services;

namespace PiholeDnsPropagate.Tests.Worker;

[TestFixture]
[SuppressMessage("MicrosoftDesign", "CA1515", Justification = "NUnit requires public fixtures for discovery.")]
public sealed class CronSchedulerServiceTests
{
    [Test]
    public async Task SchedulerInvokesCoordinatorOnInterval()
    {
        // Arrange
        var coordinator = Substitute.For<ISyncCoordinator>();
        coordinator
            .SynchronizeAsync(false, Arg.Any<CancellationToken>())
            .Returns(new SyncResult(new SyncNodeResult("primary", SyncStatus.Success, new RecordCounts(0, 0)), Array.Empty<SyncNodeResult>()));

        var options = new TestOptionsMonitor<SynchronizationOptions>(new SynchronizationOptions
        {
            Interval = TimeSpan.FromMilliseconds(50),
            DryRun = false
        });

        var syncState = new SyncState();
        using var scheduler = new CronSchedulerService(coordinator, options, syncState, NullLogger<CronSchedulerService>.Instance);

        // Act
        await scheduler.StartAsync(CancellationToken.None).ConfigureAwait(false);
        await Task.Delay(150).ConfigureAwait(false);
        await scheduler.StopAsync(CancellationToken.None).ConfigureAwait(false);

        // Assert
        await coordinator.ReceivedWithAnyArgs().SynchronizeAsync(default, default).ConfigureAwait(false);
    }
}
