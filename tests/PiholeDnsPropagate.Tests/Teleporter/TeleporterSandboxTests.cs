using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using PiholeDnsPropagate.Teleporter;
using PiholeDnsPropagate.Teleporter.Authentication;

namespace PiholeDnsPropagate.Tests.Teleporter;

[TestFixture]
[Category("Integration")]
public class TeleporterSandboxTests
{
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
}
