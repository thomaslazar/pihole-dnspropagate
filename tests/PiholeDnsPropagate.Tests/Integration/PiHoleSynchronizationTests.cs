using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using PiholeDnsPropagate.Options;
using PiholeDnsPropagate.Teleporter;
using PiholeDnsPropagate.Teleporter.Authentication;
using PiholeDnsPropagate.Tests.Common;
using PiholeDnsPropagate.Tests.Teleporter.Fixtures;
using Tomlyn;
using Tomlyn.Model;

namespace PiholeDnsPropagate.Tests.Integration;

[TestFixture]
[Category("Integration")]
[System.Diagnostics.CodeAnalysis.SuppressMessage("MicrosoftDesign", "CA1515", Justification = "NUnit requires public fixtures for discovery.")]
public sealed class PiHoleSynchronizationTests
{
    private static readonly TimeSpan OperationTimeout = TimeSpan.FromMinutes(2);
    private static readonly string[] DesiredHostRecords =
    {
        "10.53.0.10 lab-primary.local",
        "10.53.0.11 lab-web.local"
    };

    private static readonly string[] DesiredCnameRecords =
    {
        "app.lab.local,lab-web.local"
    };

    private static readonly string[] StaleSecondaryHosts =
    {
        "10.53.0.50 stale.local"
    };

    private PiHoleCluster? _cluster;
    private TeleporterArchiveProcessor? _processor;
    private TeleporterClientFactory? _clientFactory;

    [OneTimeSetUp]
    public async Task OneTimeSetup()
    {
        using var cts = new CancellationTokenSource(OperationTimeout);
        try
        {
            _cluster = await PiHoleCluster.StartAsync(cts.Token).ConfigureAwait(false);
        }
        catch (Exception ex) when (IsEnvironmentUnavailable(ex))
        {
            Assert.Inconclusive($"Pi-hole Testcontainers unavailable for integration tests: {ex.Message}");
            return;
        }

        _processor = new TeleporterArchiveProcessor();
        var sessionFactory = new PiHoleSessionFactory(NullLogger<PiHoleSessionFactory>.Instance);
        var syncOptions = new TestOptionsMonitor<SynchronizationOptions>(new SynchronizationOptions
        {
            RequestTimeout = TimeSpan.FromSeconds(60)
        });
        _clientFactory = new TeleporterClientFactory(sessionFactory, syncOptions, NullLogger<TeleporterClient>.Instance);
    }

    [OneTimeTearDown]
    public async Task OneTimeTeardown()
    {
        if (_cluster is not null)
        {
            await _cluster.DisposeAsync().ConfigureAwait(false);
        }
    }

    [Test]
    public async Task SynchronizationCopiesPrimaryRecordsToSecondary()
    {
        Assert.That(_cluster, Is.Not.Null, "Cluster must be initialized in setup.");
        Assert.That(_clientFactory, Is.Not.Null, "Client factory must be initialized in setup.");
        Assert.That(_processor, Is.Not.Null, "Processor must be initialized in setup.");

        using var cts = new CancellationTokenSource(OperationTimeout);

        // Arrange
        var primaryOptions = new PrimaryPiHoleOptions
        {
            BaseUrl = _cluster!.PrimaryBaseUri,
            Password = _cluster.Password
        };

        var secondaryOptions = new SecondaryPiHoleOptions
        {
            Nodes =
            {
                new SecondaryPiHoleNodeOptions
                {
                    Name = "secondary",
                    BaseUrl = _cluster.SecondaryBaseUri,
                    Password = _cluster.Password
                }
            }
        };

        var desiredRecords = new TeleporterDnsRecords(DesiredHostRecords, DesiredCnameRecords);

        await SeedPrimaryAsync(primaryOptions, desiredRecords, cts.Token).ConfigureAwait(false);
        var secondaryOriginal = await SeedSecondaryAsync(secondaryOptions.Nodes[0], cts.Token).ConfigureAwait(false);

        var coordinator = new SyncCoordinator(
            _clientFactory!,
            _processor!,
            new TestOptionsMonitor<PrimaryPiHoleOptions>(primaryOptions),
            new TestOptionsMonitor<SecondaryPiHoleOptions>(secondaryOptions),
            NullLogger<SyncCoordinator>.Instance);

        // Act
        var result = await coordinator.SynchronizeAsync(false, cts.Token).ConfigureAwait(false);

        // Assert
        Assert.That(result.Primary.Status, Is.EqualTo(SyncStatus.Success));
        Assert.That(result.Secondaries[0].Status, Is.EqualTo(SyncStatus.Success));

        var secondaryArchive = await DownloadArchiveAsync(_clientFactory!.CreateForSecondary(secondaryOptions.Nodes[0]), cts.Token).ConfigureAwait(false);

        ZipArchiveAssertions.AssertOnlyTomlDiffers(secondaryOriginal, secondaryArchive);

        var records = ReadDnsRecords(secondaryArchive);
        Assert.That(records.Hosts, Is.EquivalentTo(desiredRecords.Hosts));
        Assert.That(records.CnameRecords, Is.EquivalentTo(desiredRecords.CnameRecords));
    }

    private async Task SeedPrimaryAsync(PrimaryPiHoleOptions options, TeleporterDnsRecords records, CancellationToken cancellationToken)
    {
        var client = _clientFactory!.CreateForPrimary(options);
        try
        {
            var archive = await client.DownloadArchiveAsync(cancellationToken).ConfigureAwait(false);
            using var archiveStream = new MemoryStream(archive, writable: false);
            var updated = await _processor!.ReplaceDnsRecordsAsync(archiveStream, records, cancellationToken).ConfigureAwait(false);
            await client.UploadArchiveAsync(updated, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            await DisposeClientAsync(client).ConfigureAwait(false);
        }
    }

    private async Task<byte[]> SeedSecondaryAsync(SecondaryPiHoleNodeOptions node, CancellationToken cancellationToken)
    {
        var client = _clientFactory!.CreateForSecondary(node);
        try
        {
            var archive = await client.DownloadArchiveAsync(cancellationToken).ConfigureAwait(false);

            using var archiveStream = new MemoryStream(archive, writable: false);
            var modified = await _processor!.ReplaceDnsRecordsAsync(
                archiveStream,
                new TeleporterDnsRecords(StaleSecondaryHosts, Array.Empty<string>()),
                cancellationToken).ConfigureAwait(false);

            await client.UploadArchiveAsync(modified, cancellationToken).ConfigureAwait(false);
            return archive;
        }
        finally
        {
            await DisposeClientAsync(client).ConfigureAwait(false);
        }
    }

    private static async Task<byte[]> DownloadArchiveAsync(ITeleporterClient client, CancellationToken cancellationToken)
    {
        try
        {
            return await client.DownloadArchiveAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            await DisposeClientAsync(client).ConfigureAwait(false);
        }
    }

    private static async Task DisposeClientAsync(ITeleporterClient client)
    {
        if (client is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync().ConfigureAwait(false);
        }

        (client as IDisposable)?.Dispose();
    }

    private static bool IsEnvironmentUnavailable(Exception exception)
    {
        if (exception is null)
        {
            return false;
        }

        return exception switch
        {
            HttpRequestException => true,
            SocketException => true,
            TimeoutException => true,
            TaskCanceledException => true,
            AggregateException aggregate => aggregate.InnerExceptions.Any(IsEnvironmentUnavailable),
            _ => exception.InnerException is not null && IsEnvironmentUnavailable(exception.InnerException)
        };
    }

    private static TeleporterDnsRecords ReadDnsRecords(byte[] archive)
    {
        using var stream = new MemoryStream(archive, writable: false);
        using var zip = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: false);
        var tomlEntry = zip.GetEntry("etc/pihole/pihole.toml") ?? throw new InvalidOperationException("Missing pihole.toml entry.");

        using var reader = new StreamReader(tomlEntry.Open());
        var toml = reader.ReadToEnd();
        var parsed = Toml.Parse(toml).ToModel() as TomlTable;
        if (parsed is not TomlTable table || !table.TryGetValue("dns", out var dnsValue) || dnsValue is not TomlTable dnsTable)
        {
            return TeleporterDnsRecords.Empty;
        }

        static string[] ReadStringArray(TomlTable dnsTable, string key)
        {
            if (!dnsTable.TryGetValue(key, out var value) || value is not TomlArray array)
            {
                return Array.Empty<string>();
            }

            return array.OfType<string>().ToArray();
        }

        return new TeleporterDnsRecords(ReadStringArray(dnsTable, "hosts"), ReadStringArray(dnsTable, "cnameRecords"));
    }
}
