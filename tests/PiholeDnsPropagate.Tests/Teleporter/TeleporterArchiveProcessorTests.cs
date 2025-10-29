using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using NUnit.Framework;
using PiholeDnsPropagate.Teleporter;
using PiholeDnsPropagate.Tests.Teleporter.Fixtures;
using Tomlyn;
using Tomlyn.Model;

namespace PiholeDnsPropagate.Tests.Teleporter;

[TestFixture]
[System.Diagnostics.CodeAnalysis.SuppressMessage("MicrosoftDesign", "CA1515", Justification = "NUnit requires public fixtures.")]
public sealed class TeleporterArchiveProcessorTests
{
    private static readonly DateTimeOffset ReferenceTimestamp = new(2024, 01, 01, 0, 0, 0, TimeSpan.Zero);
    private static readonly string[] ExpectedHosts = { "10.0.0.1 new.local", "10.0.0.2 router.local" };
    private static readonly string[] ExpectedCnames = { "service.local,new.local" };
    private static readonly string[] ExpectedEntryNames = { "etc/hosts", "etc/pihole/dhcp.leases", "etc/pihole/pihole.toml" };

    [Test]
    public void ReplaceDnsRecordsAsyncUpdatesHostsAndCnames()
    {
        // Arrange
        var originalToml = """
[dns]
hosts = ["192.168.1.1 old.local"]
cnameRecords = ["alias.local,old.local"]
""";
        var archive = new TeleporterArchiveBuilder()
            .WithToml(originalToml, ReferenceTimestamp)
            .WithHosts("127.0.0.1 localhost", ReferenceTimestamp)
            .Build();

        var processor = new TeleporterArchiveProcessor();
        using var inputStream = new MemoryStream(archive, writable: false);

        // Act
        var result = processor.ReplaceDnsRecordsAsync(
            inputStream,
            new TeleporterDnsRecords(
                new List<string> { "10.0.0.1 new.local", "10.0.0.2 router.local" },
                new List<string> { "service.local,new.local" }),
            default).GetAwaiter().GetResult();

        // Assert
        using var outputStream = new MemoryStream(result, writable: false);
        using var zip = new ZipArchive(outputStream, ZipArchiveMode.Read, leaveOpen: true);

        var tomlEntry = zip.GetEntry("etc/pihole/pihole.toml");
        Assert.That(tomlEntry, Is.Not.Null, "Modified archive should contain pihole.toml.");

        using var tomlReader = new StreamReader(tomlEntry!.Open(), Encoding.UTF8);
        var tomlText = tomlReader.ReadToEnd();
        var parsed = Toml.Parse(tomlText).ToModel() as TomlTable;
        var dnsTable = parsed!["dns"] as TomlTable;
        var hostsArray = ((TomlArray)dnsTable!["hosts"]).OfType<string>().ToArray();
        var cnameArray = ((TomlArray)dnsTable!["cnameRecords"]).OfType<string>().ToArray();

        Assert.That(hostsArray, Is.EqualTo(ExpectedHosts));
        Assert.That(cnameArray, Is.EqualTo(ExpectedCnames));

        var hostsEntry = zip.GetEntry("etc/hosts");
        Assert.That(hostsEntry, Is.Not.Null);
        Assert.That(hostsEntry!.LastWriteTime, Is.EqualTo(ReferenceTimestamp));
        using var hostsReader = new StreamReader(hostsEntry.Open(), Encoding.UTF8);
        Assert.That(hostsReader.ReadToEnd(), Is.EqualTo("127.0.0.1 localhost"));
    }

    [Test]
    public void ReplaceDnsRecordsAsyncPreservesLineEndings()
    {
        // Arrange
        const string originalToml = "[dns]\r\nhosts = [\"192.168.1.1 old.local\"]\r\ncnameRecords = [\"alias.local,old.local\"]";
        var archive = new TeleporterArchiveBuilder()
            .WithToml(originalToml)
            .Build();

        var processor = new TeleporterArchiveProcessor();
        using var inputStream = new MemoryStream(archive, writable: false);

        // Act
        var result = processor.ReplaceDnsRecordsAsync(
            inputStream,
            new TeleporterDnsRecords(new List<string>(), new List<string>()),
            default).GetAwaiter().GetResult();

        // Assert
        using var outputStream = new MemoryStream(result, writable: false);
        using var zip = new ZipArchive(outputStream, ZipArchiveMode.Read, leaveOpen: true);
        using var reader = new StreamReader(zip.GetEntry("etc/pihole/pihole.toml")!.Open(), Encoding.UTF8);
        var text = reader.ReadToEnd();

        Assert.That(text.Contains("\r\n", StringComparison.Ordinal), Is.True);
        Assert.That(text.Contains('\r', StringComparison.Ordinal), Is.True);
        Assert.That(text.Contains("\n\n", StringComparison.Ordinal), Is.False);
    }

    [Test]
    public void ReplaceDnsRecordsAsyncPreservesArchiveStructureAndMetadata()
    {
        // Arrange
        var archive = new TeleporterArchiveBuilder()
            .WithToml("[dns]\nhosts=[]\n", ReferenceTimestamp)
            .WithHosts("127.0.0.1 localhost", ReferenceTimestamp.AddHours(1))
            .WithDhcpLeases("sample", ReferenceTimestamp.AddHours(2))
            .Build();

        var processor = new TeleporterArchiveProcessor();
        using var inputStream = new MemoryStream(archive, writable: false);

        // Act
        var result = processor.ReplaceDnsRecordsAsync(
            inputStream,
            new TeleporterDnsRecords(new List<string>(), new List<string>()),
            default).GetAwaiter().GetResult();

        // Assert
        using var outputStream = new MemoryStream(result, writable: false);
        using var zip = new ZipArchive(outputStream, ZipArchiveMode.Read, leaveOpen: true);

        var entryNames = zip.Entries.Select(e => e.FullName).OrderBy(n => n).ToArray();
        Assert.That(entryNames, Is.EqualTo(ExpectedEntryNames));

        Assert.That(zip.GetEntry("etc/hosts")!.LastWriteTime, Is.EqualTo(ReferenceTimestamp.AddHours(1)));
        Assert.That(zip.GetEntry("etc/pihole/dhcp.leases")!.LastWriteTime, Is.EqualTo(ReferenceTimestamp.AddHours(2)));
        Assert.That(zip.GetEntry("etc/pihole/pihole.toml")!.LastWriteTime, Is.EqualTo(ReferenceTimestamp));
    }

    [Test]
    public void ReplaceDnsRecordsAsyncCreatesDnsSectionWhenMissing()
    {
        // Arrange
        var archive = new TeleporterArchiveBuilder()
            .WithToml("# empty file")
            .Build();

        var processor = new TeleporterArchiveProcessor();
        using var inputStream = new MemoryStream(archive, writable: false);

        // Act
        var result = processor.ReplaceDnsRecordsAsync(
            inputStream,
            new TeleporterDnsRecords(new List<string> { "10.0.0.5 example.local" }, new List<string>()),
            default).GetAwaiter().GetResult();

        // Assert
        using var zip = new ZipArchive(new MemoryStream(result, writable: false), ZipArchiveMode.Read, leaveOpen: false);
        using var reader = new StreamReader(zip.GetEntry("etc/pihole/pihole.toml")!.Open(), Encoding.UTF8);
        var text = reader.ReadToEnd();
        Assert.That(text.Contains("[dns]", StringComparison.OrdinalIgnoreCase), Is.True);
        Assert.That(text.Contains("10.0.0.5 example.local", StringComparison.Ordinal), Is.True);
    }
}
