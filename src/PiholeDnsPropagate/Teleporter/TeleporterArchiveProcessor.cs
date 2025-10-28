using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SharpCompress.Archives.Zip;
using SharpCompress.Common;
using SharpCompress.Readers;
using SharpCompress.Writers.Zip;
using Tomlyn;
using Tomlyn.Model;

namespace PiholeDnsPropagate.Teleporter;

public sealed class TeleporterArchiveProcessor : ITeleporterArchiveProcessor
{
    private const string TomlEntryPath = "etc/pihole/pihole.toml";

    public async Task<byte[]> ReplaceDnsRecordsAsync(
        Stream archiveStream,
        TeleporterDnsRecords dnsRecords,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(archiveStream);
        ArgumentNullException.ThrowIfNull(dnsRecords);

        cancellationToken.ThrowIfCancellationRequested();

        if (!archiveStream.CanRead)
        {
            throw new ArgumentException("Archive stream must be readable.", nameof(archiveStream));
        }

        using var inputArchive = ZipArchive.Open(archiveStream, new ReaderOptions { LeaveStreamOpen = true });
        using var outputStream = new MemoryStream();
        using (var zipWriter = new ZipWriter(outputStream, new ZipWriterOptions(CompressionType.Deflate)
        {
            LeaveStreamOpen = true
        }))
        {
            foreach (var entry in inputArchive.Entries)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (entry.IsDirectory)
                {
                    continue; // Directories are inferred when writing files.
                }

                using var entryStream = entry.OpenEntryStream();
                var zipEntry = (ZipArchiveEntry)entry;
                var options = CreateEntryOptions(zipEntry);
                var entryKey = entry.Key ?? string.Empty;

                if (IsTomlEntry(entryKey))
                {
                    var processedBytes = ProcessToml(entryStream, dnsRecords);
                    using var replacementStream = new MemoryStream(processedBytes, writable: false);
                    zipWriter.Write(entryKey, replacementStream, options);
                }
                else
                {
                    zipWriter.Write(entryKey, entryStream, options);
                }
            }
        }

        return await Task.FromResult(outputStream.ToArray()).ConfigureAwait(false);
    }

    private static bool IsTomlEntry(string entryKey)
        => string.Equals(entryKey?.TrimStart('/'), TomlEntryPath, StringComparison.OrdinalIgnoreCase);

    private static byte[] ProcessToml(Stream entryStream, TeleporterDnsRecords dnsRecords)
    {
        using var reader = new StreamReader(entryStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        var originalText = reader.ReadToEnd();

        var newline = DetectNewline(originalText);
        var hadTrailingNewline = originalText.EndsWith('\n');

        var parseResult = Toml.Parse(originalText);
        if (parseResult.HasErrors)
        {
            var diagnostics = string.Join(", ", parseResult.Diagnostics);
            throw new InvalidDataException($"Failed to parse pihole.toml from Teleporter archive. {diagnostics}");
        }

        var model = parseResult.ToModel();
        if (model is not TomlTable table)
        {
            throw new InvalidDataException("Unexpected TOML document structure.");
        }

        if (!table.TryGetValue("dns", out var dnsValue) || dnsValue is not TomlTable dnsTable)
        {
            dnsTable = new TomlTable();
            table["dns"] = dnsTable;
        }

        dnsTable["hosts"] = CreateTomlArray(dnsRecords.Hosts);
        dnsTable["cnameRecords"] = CreateTomlArray(dnsRecords.CnameRecords);

        var serialized = Toml.FromModel(table);
        var normalized = NormalizeLineEndings(serialized, newline, hadTrailingNewline);
        return Encoding.UTF8.GetBytes(normalized);
    }

    private static string DetectNewline(string text)
        => text.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";

    private static string NormalizeLineEndings(string text, string newline, bool hadTrailingNewline)
    {
        var normalized = text.Replace("\r\n", "\n", StringComparison.Ordinal);

        if (!hadTrailingNewline)
        {
            normalized = normalized.TrimEnd('\n');
        }

        if (newline == "\r\n")
        {
            normalized = normalized.Replace("\n", "\r\n", StringComparison.Ordinal);
        }

        return normalized;
    }

    private static TomlArray CreateTomlArray(IReadOnlyList<string> values)
    {
        var array = new TomlArray();
        foreach (var value in values)
        {
            array.Add(value);
        }

        return array;
    }

    private static ZipWriterEntryOptions CreateEntryOptions(ZipArchiveEntry entry)
    {
        var options = new ZipWriterEntryOptions
        {
            CompressionType = entry.CompressionType
        };

        options.ModificationDateTime = entry.LastModifiedTime;

        return options;
    }
}
