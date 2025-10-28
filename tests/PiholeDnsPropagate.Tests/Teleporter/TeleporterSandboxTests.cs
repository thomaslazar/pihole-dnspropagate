using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using PiholeDnsPropagate.Teleporter;
using PiholeDnsPropagate.Teleporter.Authentication;
using Tomlyn;
using Tomlyn.Model;

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

        var archive = await client.DownloadArchiveAsync().ConfigureAwait(false);
        Assert.That(archive, Is.Not.Null.And.Not.Empty,
            "Download should yield bytes from /api/teleporter.");

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

        Assert.That(hosts, Is.EqualTo(desiredRecords.Hosts));
        Assert.That(cnames, Is.EqualTo(desiredRecords.CnameRecords));

        AssertArchiveEntriesEqualExceptToml(originalArchive, updatedArchive);
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
