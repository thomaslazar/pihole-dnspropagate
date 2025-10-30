using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PiholeDnsPropagate.Options;
using Tomlyn;
using Tomlyn.Model;

namespace PiholeDnsPropagate.Teleporter;

public sealed class SyncCoordinator : ISyncCoordinator
{
    private static readonly Action<ILogger, string, Exception?> LogNodeDownloadFailedMessage =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(6001, nameof(ProcessSecondaryAsync)),
            "sync.node.download_failed {Node}");

    private static readonly Action<ILogger, string, Exception?> LogNodeParseFailedMessage =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(6002, nameof(ProcessSecondaryAsync)),
            "sync.node.parse_failed {Node}");

    private static readonly Action<ILogger, string, int, int, int, int, Exception?> LogNodeDryRunMessage =
        LoggerMessage.Define<string, int, int, int, int>(LogLevel.Information, new EventId(6003, nameof(ProcessSecondaryAsync)),
            "sync.node.dry_run {Node} {BeforeHosts} {AfterHosts} {BeforeCnames} {AfterCnames}");

    private static readonly Action<ILogger, string, int, int, int, int, Exception?> LogNodeSuccessMessage =
        LoggerMessage.Define<string, int, int, int, int>(LogLevel.Information, new EventId(6004, nameof(ProcessSecondaryAsync)),
            "sync.node.success {Node} {BeforeHosts} {AfterHosts} {BeforeCnames} {AfterCnames}");

    private static readonly Action<ILogger, string, Exception?> LogNodeApplyFailedMessage =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(6005, nameof(ProcessSecondaryAsync)),
            "sync.node.apply_failed {Node}");

    private static readonly Action<ILogger, string, Exception?> LogSummaryMessage =
        LoggerMessage.Define<string>(LogLevel.Information, new EventId(6006, nameof(LogSummary)),
            "sync.summary {Payload}");

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    private readonly ITeleporterClientFactory _clientFactory;
    private readonly ITeleporterArchiveProcessor _archiveProcessor;
    private readonly IOptionsMonitor<PrimaryPiHoleOptions> _primaryOptions;
    private readonly IOptionsMonitor<SecondaryPiHoleOptions> _secondaryOptions;
    private readonly ILogger<SyncCoordinator> _logger;

    public SyncCoordinator(
        ITeleporterClientFactory clientFactory,
        ITeleporterArchiveProcessor archiveProcessor,
        IOptionsMonitor<PrimaryPiHoleOptions> primaryOptions,
        IOptionsMonitor<SecondaryPiHoleOptions> secondaryOptions,
        ILogger<SyncCoordinator> logger)
    {
        _clientFactory = clientFactory;
        _archiveProcessor = archiveProcessor;
        _primaryOptions = primaryOptions;
        _secondaryOptions = secondaryOptions;
        _logger = logger;
    }

    public async Task<SyncResult> SynchronizeAsync(bool dryRun, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var primaryOpts = _primaryOptions.CurrentValue;
        var secondaryOpts = _secondaryOptions.CurrentValue;

        var primaryRecords = await FetchPrimaryRecordsAsync(primaryOpts, cancellationToken).ConfigureAwait(false);
        var primaryResult = new SyncNodeResult(
            "primary",
            SyncStatus.Success,
            Before: new RecordCounts(primaryRecords.Hosts.Count, primaryRecords.CnameRecords.Count));

        var secondaryResults = new List<SyncNodeResult>();
        foreach (var node in secondaryOpts.Nodes)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = await ProcessSecondaryAsync(node, primaryRecords, dryRun, cancellationToken).ConfigureAwait(false);
            secondaryResults.Add(result);
        }

        var syncResult = new SyncResult(primaryResult, secondaryResults);
        LogSummary(syncResult, dryRun);
        return syncResult;
    }

    private async Task<TeleporterDnsRecords> FetchPrimaryRecordsAsync(PrimaryPiHoleOptions options, CancellationToken cancellationToken)
    {
        var client = _clientFactory.CreateForPrimary(options);
        try
        {
            var archive = await client.DownloadArchiveAsync(cancellationToken).ConfigureAwait(false);
            return ExtractRecords(archive);
        }
        finally
        {
            await DisposeClientAsync(client).ConfigureAwait(false);
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031", Justification = "Coordinator must continue when individual nodes fail.")]
    private async Task<SyncNodeResult> ProcessSecondaryAsync(
        SecondaryPiHoleNodeOptions node,
        TeleporterDnsRecords desired,
        bool dryRun,
        CancellationToken cancellationToken)
    {
        var client = _clientFactory.CreateForSecondary(node);
        try
        {
            byte[] archive;
            try
            {
                archive = await client.DownloadArchiveAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                LogNodeDownloadFailedMessage(_logger, node.Name, ex);
                return new SyncNodeResult(node.Name, SyncStatus.Failed, Error: ex.Message);
            }

            TeleporterDnsRecords current;
            try
            {
                current = ExtractRecords(archive);
            }
            catch (InvalidDataException ex)
            {
                LogNodeParseFailedMessage(_logger, node.Name, ex);
                return new SyncNodeResult(node.Name, SyncStatus.Failed, Error: ex.Message);
            }

            var beforeCounts = new RecordCounts(current.Hosts.Count, current.CnameRecords.Count);
            var afterCounts = new RecordCounts(desired.Hosts.Count, desired.CnameRecords.Count);

            if (dryRun)
            {
                LogNodeDryRunMessage(_logger, node.Name, beforeCounts.Hosts, afterCounts.Hosts, beforeCounts.Cnames, afterCounts.Cnames, null);
                return new SyncNodeResult(node.Name, SyncStatus.Skipped, beforeCounts, afterCounts);
            }

            try
            {
                using var inputStream = new MemoryStream(archive, writable: false);
                var processed = await _archiveProcessor.ReplaceDnsRecordsAsync(inputStream, desired, cancellationToken).ConfigureAwait(false);
                await client.UploadArchiveAsync(processed, cancellationToken).ConfigureAwait(false);
                LogNodeSuccessMessage(_logger, node.Name, beforeCounts.Hosts, afterCounts.Hosts, beforeCounts.Cnames, afterCounts.Cnames, null);
                return new SyncNodeResult(node.Name, SyncStatus.Success, beforeCounts, afterCounts);
            }
            catch (Exception ex)
            {
                LogNodeApplyFailedMessage(_logger, node.Name, ex);
                return new SyncNodeResult(node.Name, SyncStatus.Failed, beforeCounts, Error: ex.Message);
            }
        }
        finally
        {
            await DisposeClientAsync(client).ConfigureAwait(false);
        }
    }

    private static async ValueTask DisposeClientAsync(ITeleporterClient client)
    {
        switch (client)
        {
            case IAsyncDisposable asyncDisposable:
                await asyncDisposable.DisposeAsync().ConfigureAwait(false);
                break;
            case IDisposable disposable:
                disposable.Dispose();
                break;
        }
    }

    private static TeleporterDnsRecords ExtractRecords(byte[] archive)
    {
        using var archiveStream = new MemoryStream(archive, writable: false);
        using var zip = new ZipArchive(archiveStream, ZipArchiveMode.Read, leaveOpen: false);
        var entry = zip.GetEntry("etc/pihole/pihole.toml") ?? zip.GetEntry("pihole/pihole.toml")
            ?? throw new InvalidDataException("Teleporter archive missing etc/pihole/pihole.toml");

        using var reader = new StreamReader(entry.Open(), Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: false);
        var tomlText = reader.ReadToEnd();
        var parseResult = Toml.Parse(tomlText);
        if (parseResult.HasErrors)
        {
            var diagnostics = string.Join(", ", parseResult.Diagnostics);
            throw new InvalidDataException($"Failed to parse pihole.toml. {diagnostics}");
        }

        var model = parseResult.ToModel();
        if (model is not TomlTable table)
        {
            throw new InvalidDataException("Unexpected TOML structure.");
        }

        if (!table.TryGetValue("dns", out var dnsValue) || dnsValue is not TomlTable dnsTable)
        {
            return TeleporterDnsRecords.Empty;
        }

        var hosts = ReadArray(dnsTable, "hosts");
        var cnames = ReadArray(dnsTable, "cnameRecords");
        return new TeleporterDnsRecords(hosts, cnames);
    }

    private static string[] ReadArray(TomlTable table, string key)
    {
        if (!table.TryGetValue(key, out var value) || value is not TomlArray array)
        {
            return Array.Empty<string>();
        }

        return array.OfType<string>().ToArray();
    }

    private void LogSummary(SyncResult result, bool dryRun)
    {
        var payload = new
        {
            dryRun,
            primary = new
            {
                result.Primary.Status,
                result.Primary.Before?.Hosts,
                result.Primary.Before?.Cnames
            },
            secondaries = result.Secondaries.Select(node => new
            {
                node.NodeName,
                node.Status,
                Before = node.Before,
                After = node.After,
                node.Error
            })
        };

        LogSummaryMessage(_logger, JsonSerializer.Serialize(payload, JsonOptions), null);
    }
}
