using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace PiholeDnsPropagate.Tests.Teleporter.Fixtures;

internal sealed class TeleporterArchiveBuilder
{
    private readonly List<Entry> _entries = new();

    public TeleporterArchiveBuilder WithToml(string content, DateTimeOffset? modified = null)
        => WithFile("etc/pihole/pihole.toml", content, modified);

    public TeleporterArchiveBuilder WithHosts(string content = "127.0.0.1 localhost", DateTimeOffset? modified = null)
        => WithFile("etc/hosts", content, modified);

    public TeleporterArchiveBuilder WithDhcpLeases(string content = "", DateTimeOffset? modified = null)
        => WithFile("etc/pihole/dhcp.leases", content, modified);

    public TeleporterArchiveBuilder WithFile(string path, string content, DateTimeOffset? modified = null)
        => WithFile(path, Encoding.UTF8.GetBytes(content), modified);

    public TeleporterArchiveBuilder WithFile(string path, byte[] content, DateTimeOffset? modified = null)
    {
        _entries.Add(new Entry(path, content, modified ?? DefaultTimestamp));
        return this;
    }

    public byte[] Build()
    {
        using var buffer = new MemoryStream();
        using (var archive = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var entry in _entries)
            {
                var zipEntry = archive.CreateEntry(entry.Path, CompressionLevel.Optimal);
                zipEntry.LastWriteTime = entry.Modified;
                using var stream = zipEntry.Open();
                stream.Write(entry.Content, 0, entry.Content.Length);
            }
        }

        return buffer.ToArray();
    }

    private static readonly DateTimeOffset DefaultTimestamp = new(2024, 01, 01, 0, 0, 0, TimeSpan.Zero);

    private sealed record Entry(string Path, byte[] Content, DateTimeOffset Modified);
}
