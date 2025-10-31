using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using NUnit.Framework;

namespace PiholeDnsPropagate.Tests.Common;

internal static class ZipArchiveAssertions
{
    private const string TomlEntry = "etc/pihole/pihole.toml";

    public static void AssertOnlyTomlDiffers(byte[] original, byte[] updated)
    {
        using var originalStream = new MemoryStream(original, writable: false);
        using var updatedStream = new MemoryStream(updated, writable: false);
        using var originalZip = new ZipArchive(originalStream, ZipArchiveMode.Read, leaveOpen: false);
        using var updatedZip = new ZipArchive(updatedStream, ZipArchiveMode.Read, leaveOpen: false);

        var originalEntries = originalZip.Entries.ToDictionary(e => e.FullName, StringComparer.OrdinalIgnoreCase);
        var updatedEntries = updatedZip.Entries.ToDictionary(e => e.FullName, StringComparer.OrdinalIgnoreCase);

        foreach (var (entryName, originalEntry) in originalEntries)
        {
            Assert.That(updatedEntries.ContainsKey(entryName), Is.True, $"Missing entry '{entryName}' in updated archive.");
            var updatedEntry = updatedEntries[entryName];

            if (string.Equals(entryName, TomlEntry, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            Assert.That(updatedEntry.Length, Is.EqualTo(originalEntry.Length),
                $"Entry '{entryName}' length changed unexpectedly.");
            Assert.That(updatedEntry.LastWriteTime, Is.EqualTo(originalEntry.LastWriteTime),
                $"Entry '{entryName}' timestamp changed unexpectedly.");

            using var originalEntryStream = originalEntry.Open();
            using var updatedEntryStream = updatedEntry.Open();
            AssertStreamsEqual(entryName, originalEntryStream, updatedEntryStream);
        }

        var extraEntries = updatedEntries.Keys
            .Except(originalEntries.Keys, StringComparer.OrdinalIgnoreCase)
            .Where(name => !string.Equals(name, TomlEntry, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        Assert.That(extraEntries, Is.Empty, "Updated archive contains unexpected entries.");
    }

    private static void AssertStreamsEqual(string entryName, Stream expected, Stream actual)
    {
        using var expectedBuffer = new MemoryStream();
        using var actualBuffer = new MemoryStream();
        expected.CopyTo(expectedBuffer);
        actual.CopyTo(actualBuffer);

        var expectedBytes = expectedBuffer.ToArray();
        var actualBytes = actualBuffer.ToArray();

        Assert.That(actualBytes, Is.EqualTo(expectedBytes),
            $"Entry '{entryName}' content changed unexpectedly.");
    }
}
