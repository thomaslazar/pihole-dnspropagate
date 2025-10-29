using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using PiholeDnsPropagate.Options;
using PiholeDnsPropagate.Teleporter;
using PiholeDnsPropagate.Tests.Teleporter.Fixtures;
using NSubstitute;

namespace PiholeDnsPropagate.Tests.Teleporter;

[TestFixture]
[System.Diagnostics.CodeAnalysis.SuppressMessage("MicrosoftDesign", "CA1515", Justification = "NUnit requires public fixtures.")]
public class SyncCoordinatorTests
{
    [Test]
    public async Task SynchronizeAsyncDryRunDoesNotUploadAndLogsSummary()
    {
        var primaryArchive = BuildArchive(new List<string> { "192.168.1.10 primary.local" }, new List<string>());
        var secondaryArchive = BuildArchive(new List<string> { "192.168.1.11 old.local" }, new List<string> { "alias.local,old.local" });

        var primaryOptions = new PrimaryPiHoleOptions { BaseUrl = new Uri("http://primary"), Password = "secret" };
        var secondaryOptions = new SecondaryPiHoleOptions
        {
            Nodes =
            {
                new SecondaryPiHoleNodeOptions { Name = "secondary-1", BaseUrl = new Uri("http://secondary1"), Password = "secret" }
            }
        };

        using var primaryClient = new FakeTeleporterClient(primaryArchive);
        using var secondaryClient = new FakeTeleporterClient(secondaryArchive);

        var factory = Substitute.For<ITeleporterClientFactory>();
        factory.CreateForPrimary(Arg.Any<PrimaryPiHoleOptions>()).Returns(primaryClient);
        factory.CreateForSecondary(Arg.Is<SecondaryPiHoleNodeOptions>(n => n.Name == "secondary-1")).Returns(secondaryClient);

        var archiveProcessor = new TeleporterArchiveProcessor();
        var coordinator = new SyncCoordinator(
            factory,
            archiveProcessor,
            new TestOptionsMonitor<PrimaryPiHoleOptions>(primaryOptions),
            new TestOptionsMonitor<SecondaryPiHoleOptions>(secondaryOptions),
            new ListLogger<SyncCoordinator>());

        var result = await coordinator.SynchronizeAsync(dryRun: true).ConfigureAwait(false);

        Assert.That(result.Secondaries.Single().Status, Is.EqualTo(SyncStatus.Skipped));
        Assert.That(secondaryClient.UploadInvoked, Is.False);
    }

    [Test]
    public async Task SynchronizeAsyncContinuesAfterFailures()
    {
        var primaryArchive = BuildArchive(new List<string> { "10.0.0.1 main.local" }, new List<string>());
        var failingArchive = BuildArchive(new List<string>(), new List<string>());
        var successArchive = BuildArchive(new List<string>(), new List<string>());

        var primaryOptions = new PrimaryPiHoleOptions { BaseUrl = new Uri("http://primary"), Password = "secret" };
        var secondaryOptions = new SecondaryPiHoleOptions
        {
            Nodes =
            {
                new SecondaryPiHoleNodeOptions { Name = "failing", BaseUrl = new Uri("http://secondary-fail"), Password = "secret" },
                new SecondaryPiHoleNodeOptions { Name = "second", BaseUrl = new Uri("http://secondary-ok"), Password = "secret" }
            }
        };

        using var primaryClient = new FakeTeleporterClient(primaryArchive);
        using var failingClient = new FakeTeleporterClient(failingArchive, throwOnUpload: true);
        using var successClient = new FakeTeleporterClient(successArchive);

        var factory = Substitute.For<ITeleporterClientFactory>();
        factory.CreateForPrimary(Arg.Any<PrimaryPiHoleOptions>()).Returns(primaryClient);
        factory.CreateForSecondary(Arg.Is<SecondaryPiHoleNodeOptions>(n => n.Name == "failing")).Returns(failingClient);
        factory.CreateForSecondary(Arg.Is<SecondaryPiHoleNodeOptions>(n => n.Name == "second")).Returns(successClient);

        var archiveProcessor = new TeleporterArchiveProcessor();
        var logger = new ListLogger<SyncCoordinator>();
        var coordinator = new SyncCoordinator(
            factory,
            archiveProcessor,
            new TestOptionsMonitor<PrimaryPiHoleOptions>(primaryOptions),
            new TestOptionsMonitor<SecondaryPiHoleOptions>(secondaryOptions),
            logger);

        var result = await coordinator.SynchronizeAsync(dryRun: false).ConfigureAwait(false);

        var failingResult = result.Secondaries.Single(r => r.NodeName == "failing");
        var successResult = result.Secondaries.Single(r => r.NodeName == "second");

        Assert.That(failingResult.Status, Is.EqualTo(SyncStatus.Failed));
        Assert.That(successResult.Status, Is.EqualTo(SyncStatus.Success));
        Assert.That(successClient.UploadInvoked, Is.True);
        Assert.That(logger.Entries.Any(e => e.Message.Contains("sync.node.apply_failed", StringComparison.Ordinal)), Is.True);
    }

    private static byte[] BuildArchive(IReadOnlyList<string> hosts, IReadOnlyList<string> cnames)
    {
        var tomlBuilder = new StringBuilder();
        tomlBuilder.AppendLine("[dns]");
        tomlBuilder.Append("hosts = [");
        tomlBuilder.Append(string.Join(",", hosts.Select(h => $"\"{h}\"")));
        tomlBuilder.AppendLine("]");
        tomlBuilder.Append("cnameRecords = [");
        tomlBuilder.Append(string.Join(",", cnames.Select(c => $"\"{c}\"")));
        tomlBuilder.AppendLine("]");

        return new TeleporterArchiveBuilder()
            .WithToml(tomlBuilder.ToString())
            .Build();
    }
}
