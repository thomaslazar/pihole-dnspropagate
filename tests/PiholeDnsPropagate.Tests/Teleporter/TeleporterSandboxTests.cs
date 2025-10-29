using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using PiholeDnsPropagate.Options;
using PiholeDnsPropagate.Teleporter;
using PiholeDnsPropagate.Teleporter.Authentication;
using Tomlyn;
using Tomlyn.Model;
using PiholeDnsPropagate.Tests.Teleporter.Fixtures;

namespace PiholeDnsPropagate.Tests.Teleporter;

[TestFixture]
[Category("Integration")]
[System.Diagnostics.CodeAnalysis.SuppressMessage("MicrosoftDesign", "CA1515", Justification = "NUnit requires public fixtures.")]
public class TeleporterSandboxTests
{
    private static readonly string[] SecondaryHostRecords = { "10.10.0.10 lab-host.local", "10.10.0.11 db.local" };
    private static readonly string[] SecondaryCnameRecords = { "service.lab.local,lab-host.local" };

    [Test]
    [Explicit("Requires running Pi-hole sandbox and SANDBOX_PIHOLE_URL/password variables.")]
    public async Task DownloadAndUploadArchiveAgainstSandbox()
    {
        // Arrange
        var baseUrlText = Environment.GetEnvironmentVariable("SANDBOX_PIHOLE_URL");
        var password = Environment.GetEnvironmentVariable("SANDBOX_PIHOLE_PASSWORD");

        if (string.IsNullOrWhiteSpace(baseUrlText) || string.IsNullOrWhiteSpace(password))
        {
            Assert.Inconclusive("SANDBOX_PIHOLE_URL and SANDBOX_PIHOLE_PASSWORD must be set.");
        }

        using var loggerFactory = LoggerFactory.Create(static _ => { });

        using var client = new TeleporterClient(
            new TeleporterClientOptions
            {
                InstanceName = "sandbox",
                BaseUrl = new Uri(baseUrlText!, UriKind.Absolute),
                Password = password!,
                RequestTimeout = TimeSpan.FromSeconds(30)
            },
            new PiHoleSessionFactory(loggerFactory.CreateLogger<PiHoleSessionFactory>()),
            loggerFactory.CreateLogger<TeleporterClient>());

        // Act
        var archive = await client.DownloadArchiveAsync().ConfigureAwait(false);
        Assert.That(archive, Is.Not.Null.And.Not.Empty,
            "Download should yield bytes from /api/teleporter.");

        // Assert
        try
        {
            using var archiveStream = new MemoryStream(archive, writable: false);
            using var zip = new ZipArchive(archiveStream, ZipArchiveMode.Read, leaveOpen: true);
            var tomlEntry = zip.GetEntry("etc/pihole/pihole.toml");
            if (tomlEntry is null)
            {
                var preview = Encoding.UTF8.GetString(archive);
                Assert.Fail($"Unexpected Teleporter payload: {preview}");
            }
        }
        catch (InvalidDataException ex)
        {
            var preview = Encoding.UTF8.GetString(archive);
            Assert.Fail($"Invalid Teleporter archive: {ex.Message}. Payload preview: {preview}");
        }

        Assert.DoesNotThrowAsync(async () =>
            await client.UploadArchiveAsync(archive).ConfigureAwait(false));
    }

    [Test]
    [Explicit("Requires sandbox with primary & secondary Pi-hole running (see docs/pihole-sandbox.md)."), Category("Integration")]
    public async Task ProcessArchiveAndApplyToSecondary()
    {
        // Arrange
        var secondaryUrl = Environment.GetEnvironmentVariable("SANDBOX_PIHOLE_SECONDARY_URL")
                           ?? Environment.GetEnvironmentVariable("SANDBOX_PIHOLE_URL");
        var password = Environment.GetEnvironmentVariable("SANDBOX_PIHOLE_PASSWORD");

        if (string.IsNullOrWhiteSpace(secondaryUrl) || string.IsNullOrWhiteSpace(password))
        {
            Assert.Inconclusive("SANDBOX_PIHOLE_SECONDARY_URL and SANDBOX_PIHOLE_PASSWORD must be set.");
        }

        using var loggerFactory = LoggerFactory.Create(static _ => { });
        using var client = new TeleporterClient(
            new TeleporterClientOptions
            {
                InstanceName = "secondary",
                BaseUrl = new Uri(secondaryUrl!, UriKind.Absolute),
                Password = password!,
                RequestTimeout = TimeSpan.FromSeconds(30)
            },
            new PiHoleSessionFactory(loggerFactory.CreateLogger<PiHoleSessionFactory>()),
            loggerFactory.CreateLogger<TeleporterClient>());

        var processor = new TeleporterArchiveProcessor();
        var desiredRecords = new TeleporterDnsRecords(SecondaryHostRecords, SecondaryCnameRecords);

        // Act
        var originalArchive = await client.DownloadArchiveAsync().ConfigureAwait(false);
        using var originalStream = new MemoryStream(originalArchive, writable: false);
        var processedArchive = await processor.ReplaceDnsRecordsAsync(originalStream, desiredRecords).ConfigureAwait(false);

        await client.UploadArchiveAsync(processedArchive).ConfigureAwait(false);

        var updatedArchive = await client.DownloadArchiveAsync().ConfigureAwait(false);
        using var updatedStream = new MemoryStream(updatedArchive, writable: false);
        using var zip = new ZipArchive(updatedStream, ZipArchiveMode.Read, leaveOpen: false);
        var tomlEntry = zip.GetEntry("etc/pihole/pihole.toml");
        Assert.That(tomlEntry, Is.Not.Null, "Updated archive must contain pihole.toml");

        using var reader = new StreamReader(tomlEntry!.Open(), Encoding.UTF8);
        var tomlText = reader.ReadToEndAsync().GetAwaiter().GetResult();
        var parsed = Toml.Parse(tomlText).ToModel() as TomlTable;
        var dnsTable = parsed!["dns"] as TomlTable;
        var hosts = ((TomlArray)dnsTable!["hosts"]).OfType<string>().ToArray();
        var cnames = ((TomlArray)dnsTable!["cnameRecords"]).OfType<string>().ToArray();

        // Assert
        Assert.That(hosts, Is.EqualTo(desiredRecords.Hosts));
        Assert.That(cnames, Is.EqualTo(desiredRecords.CnameRecords));

        AssertArchiveEntriesEqualExceptToml(originalArchive, updatedArchive);
    }

    [Test]
    [Explicit("Requires sandbox running primary & secondary Pi-hole; see docs."), Category("Integration")]
    public async Task SyncCoordinatorAppliesChangesAcrossSecondaries()
    {
        // Arrange
        var primaryUrl = Environment.GetEnvironmentVariable("SANDBOX_PIHOLE_PRIMARY_URL")
                         ?? Environment.GetEnvironmentVariable("SANDBOX_PIHOLE_URL");
        var secondaryUrl = Environment.GetEnvironmentVariable("SANDBOX_PIHOLE_SECONDARY_URL");
        var password = Environment.GetEnvironmentVariable("SANDBOX_PIHOLE_PASSWORD");

        if (string.IsNullOrWhiteSpace(primaryUrl) || string.IsNullOrWhiteSpace(secondaryUrl) || string.IsNullOrWhiteSpace(password))
        {
            Assert.Inconclusive("SANDBOX_PIHOLE_PRIMARY_URL, SANDBOX_PIHOLE_SECONDARY_URL, and SANDBOX_PIHOLE_PASSWORD must be set.");
        }

        using var loggerFactory = LoggerFactory.Create(builder => { });
        var sessionFactory = new PiHoleSessionFactory(loggerFactory.CreateLogger<PiHoleSessionFactory>());
        var syncOptions = new TestOptionsMonitor<SynchronizationOptions>(new SynchronizationOptions());
        var clientFactory = new TeleporterClientFactory(sessionFactory, syncOptions, loggerFactory.CreateLogger<TeleporterClient>());
        var archiveProcessor = new TeleporterArchiveProcessor();

        var primaryOptions = new PrimaryPiHoleOptions { BaseUrl = new Uri(primaryUrl!, UriKind.Absolute), Password = password! };
        var secondaryOptions = new SecondaryPiHoleOptions
        {
            Nodes =
            {
                new SecondaryPiHoleNodeOptions { Name = "secondary", BaseUrl = new Uri(secondaryUrl!, UriKind.Absolute), Password = password! }
            }
        };

        await WaitForTeleporterAsync(primaryUrl!).ConfigureAwait(false);
        await WaitForTeleporterAsync(secondaryUrl!).ConfigureAwait(false);

        var primaryClient = clientFactory.CreateForPrimary(primaryOptions);
        TeleporterDnsRecords primaryRecords;
        try
        {
            var primaryArchive = await primaryClient.DownloadArchiveAsync().ConfigureAwait(false);
            primaryRecords = ParseRecords(primaryArchive);
        }
        finally
        {
            (primaryClient as IDisposable)?.Dispose();
        }

        // Seed secondary with divergent records to ensure coordinator applies updates.
        var secondaryClient = clientFactory.CreateForSecondary(secondaryOptions.Nodes.Single());
        try
        {
            var secondaryArchive = await secondaryClient.DownloadArchiveAsync().ConfigureAwait(false);
            using var secondaryStream = new MemoryStream(secondaryArchive, writable: false);
            var modified = await archiveProcessor.ReplaceDnsRecordsAsync(secondaryStream,
                new TeleporterDnsRecords(new List<string> { "192.168.20.5 stale.local" }, new List<string>()), CancellationToken.None).ConfigureAwait(false);
            await secondaryClient.UploadArchiveAsync(modified).ConfigureAwait(false);
        }
        finally
        {
            (secondaryClient as IDisposable)?.Dispose();
        }

        await Task.Delay(TimeSpan.FromSeconds(5)).ConfigureAwait(false);

        // Act
        var coordinator = new SyncCoordinator(
            clientFactory,
            archiveProcessor,
            new TestOptionsMonitor<PrimaryPiHoleOptions>(primaryOptions),
            new TestOptionsMonitor<SecondaryPiHoleOptions>(secondaryOptions),
            loggerFactory.CreateLogger<SyncCoordinator>());

        var result = await coordinator.SynchronizeAsync(dryRun: false).ConfigureAwait(false);
        TestContext.WriteLine(System.Text.Json.JsonSerializer.Serialize(result));

        // Assert
        Assert.That(result.Secondaries.Single().Status, Is.EqualTo(SyncStatus.Success));

        secondaryClient = clientFactory.CreateForSecondary(secondaryOptions.Nodes.Single());
        try
        {
            var archive = await secondaryClient.DownloadArchiveAsync().ConfigureAwait(false);
            var records = ParseRecords(archive);
            Assert.That(records.Hosts, Is.EquivalentTo(primaryRecords.Hosts));
            Assert.That(records.CnameRecords, Is.EquivalentTo(primaryRecords.CnameRecords));
        }
        finally
        {
            (secondaryClient as IDisposable)?.Dispose();
        }
    }

    private static TeleporterDnsRecords ParseRecords(byte[] archive)
    {
        using var archiveStream = new MemoryStream(archive, writable: false);
        using var zip = new ZipArchive(archiveStream, ZipArchiveMode.Read, leaveOpen: false);
        var entry = zip.GetEntry("etc/pihole/pihole.toml") ?? zip.GetEntry("pihole/pihole.toml")
            ?? throw new InvalidDataException("Missing pihole.toml in archive");

        using var reader = new StreamReader(entry.Open(), Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: false);
        var toml = reader.ReadToEnd();
        var model = Toml.Parse(toml).ToModel() as TomlTable;
        if (model is not TomlTable table || !table.TryGetValue("dns", out var dnsValue) || dnsValue is not TomlTable dnsTable)
        {
            return TeleporterDnsRecords.Empty;
        }

        static string[] ReadArray(TomlTable table, string key)
        {
            if (!table.TryGetValue(key, out var value) || value is not TomlArray array)
            {
                return Array.Empty<string>();
            }

            return array.OfType<string>().ToArray();
        }

        return new TeleporterDnsRecords(ReadArray(dnsTable, "hosts"), ReadArray(dnsTable, "cnameRecords"));
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031", Justification = "Polling helper tolerates transient errors while waiting for sandbox readiness.")]
    private static async Task WaitForTeleporterAsync(string baseUrl)
    {
        using var httpClient = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        var attempts = 0;
        while (attempts < 60)
        {
            try
            {
                using var response = await httpClient.GetAsync(new Uri(new Uri(baseUrl, UriKind.Absolute), "/api/teleporter"), HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
                if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    return;
                }
            }
            catch (HttpRequestException)
            {
                // ignore and retry
            }
            catch (TaskCanceledException)
            {
                // ignore and retry
            }

            await Task.Delay(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
            attempts++;
        }
    }

    private static void AssertArchiveEntriesEqualExceptToml(byte[] original, byte[] updated)
    {
        using var originalZip = new ZipArchive(new MemoryStream(original, writable: false), ZipArchiveMode.Read, leaveOpen: false);
        using var updatedZip = new ZipArchive(new MemoryStream(updated, writable: false), ZipArchiveMode.Read, leaveOpen: false);

        var originalEntries = originalZip.Entries.Where(e => !e.FullName.Equals("etc/pihole/pihole.toml", StringComparison.OrdinalIgnoreCase));
        var updatedEntries = updatedZip.Entries.Where(e => !e.FullName.Equals("etc/pihole/pihole.toml", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(e => e.FullName, e => e, StringComparer.OrdinalIgnoreCase);

        foreach (var entry in originalEntries)
        {
            Assert.That(updatedEntries.ContainsKey(entry.FullName),
                $"Expected entry '{entry.FullName}' to exist in processed archive.");

            using var originalStream = entry.Open();
            using var updatedStream = updatedEntries[entry.FullName].Open();
            Assert.That(StreamContentsEqual(originalStream, updatedStream),
                $"Entry '{entry.FullName}' should remain unchanged.");
        }
    }

    private static bool StreamContentsEqual(Stream first, Stream second)
    {
        const int bufferSize = 81920;
        var firstBuffer = new byte[bufferSize];
        var secondBuffer = new byte[bufferSize];

        while (true)
        {
            var firstRead = first.Read(firstBuffer, 0, bufferSize);
            var secondRead = second.Read(secondBuffer, 0, bufferSize);

            if (firstRead != secondRead)
            {
                return false;
            }

            if (firstRead == 0)
            {
                return true;
            }

            for (var i = 0; i < firstRead; i++)
            {
                if (firstBuffer[i] != secondBuffer[i])
                {
                    return false;
                }
            }
        }
    }
}
