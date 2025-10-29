using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Spectre.Console.Cli;
using PiholeDnsPropagate.Teleporter;
using PiholeDnsPropagate.Worker.Services;
using TeleporterSyncStatus = PiholeDnsPropagate.Teleporter.SyncStatus;

namespace PiholeDnsPropagate.Worker.Scheduling;

[SuppressMessage("MicrosoftDesign", "CA1515", Justification = "Exposed via CLI entry point.")]
public sealed class ManualSyncCommand : AsyncCommand<ManualSyncCommandSettings>
{
    private static readonly Action<ILogger, Exception?> LogOverlapMessage =
        LoggerMessage.Define(LogLevel.Warning, new EventId(7101, nameof(ManualSyncCommand)),
            "cli.sync.rejected overlap");

    private static readonly Action<ILogger, Exception?> LogSuccessMessage =
        LoggerMessage.Define(LogLevel.Information, new EventId(7102, nameof(ManualSyncCommand)),
            "cli.sync.success");

    private static readonly Action<ILogger, Exception?> LogPartialFailureMessage =
        LoggerMessage.Define(LogLevel.Error, new EventId(7103, nameof(ManualSyncCommand)),
            "cli.sync.partial_failure");

    private static readonly Action<ILogger, Exception?> LogFailureMessage =
        LoggerMessage.Define(LogLevel.Error, new EventId(7104, nameof(ManualSyncCommand)),
            "cli.sync.failed");

    private readonly ISyncCoordinator _coordinator;
    private readonly ISyncState _syncState;
    private readonly ILogger<ManualSyncCommand> _logger;

    public ManualSyncCommand(ISyncCoordinator coordinator, ISyncState syncState, ILogger<ManualSyncCommand> logger)
    {
        _coordinator = coordinator;
        _syncState = syncState;
        _logger = logger;
    }

    [SuppressMessage("Design", "CA1031", Justification = "Manual sync must convert unexpected errors into exit codes without crashing the CLI.")]
    public override async Task<int> ExecuteAsync(CommandContext? context, ManualSyncCommandSettings settings, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (!_syncState.TryMarkRunning())
        {
            LogOverlapMessage(_logger, null);
            return -1;
        }

        try
        {
            var result = await _coordinator.SynchronizeAsync(settings.DryRun, cancellationToken).ConfigureAwait(false);
            var allSuccess = result.Secondaries.All(s => s.Status != TeleporterSyncStatus.Failed);
            if (allSuccess)
            {
                _syncState.MarkSuccess(DateTimeOffset.UtcNow);
                LogSuccessMessage(_logger, null);
                return 0;
            }

            _syncState.MarkFailure(DateTimeOffset.UtcNow);
            LogPartialFailureMessage(_logger, null);
            return 2;
        }
        catch (Exception ex)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                _syncState.MarkFailure(DateTimeOffset.UtcNow);
                LogFailureMessage(_logger, ex);
                return 1;
            }

            return -1;
        }
        finally
        {
            _syncState.MarkIdle();
        }
    }

}

[SuppressMessage("MicrosoftDesign", "CA1515", Justification = "Exposed for CLI usage.")]
public sealed class ManualSyncCommandSettings : CommandSettings
{
    [CommandOption("--dry-run")]
    [DefaultValue(false)]
    public bool DryRun { get; init; }
}
