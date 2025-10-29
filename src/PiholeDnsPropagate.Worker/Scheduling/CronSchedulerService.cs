using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cronos;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PiholeDnsPropagate.Options;
using PiholeDnsPropagate.Teleporter;
using PiholeDnsPropagate.Worker.Services;
using TeleporterSyncStatus = PiholeDnsPropagate.Teleporter.SyncStatus;

namespace PiholeDnsPropagate.Worker.Scheduling;

internal sealed class CronSchedulerService : BackgroundService
{
    private static readonly Action<ILogger, TimeSpan, Exception?> LogNextRunMessage =
        LoggerMessage.Define<TimeSpan>(LogLevel.Information, new EventId(7001, nameof(CronSchedulerService)),
            "scheduler.next_run_in {Delay}");

    private static readonly Action<ILogger, string, Exception?> LogSyncFailedMessage =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(7002, nameof(CronSchedulerService)),
            "scheduler.sync_failed {Message}");

    private static readonly Action<ILogger, string, Exception?> LogInvalidCronMessage =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(7003, nameof(CronSchedulerService)),
            "scheduler.invalid_cron {Expression}");

    private static readonly Action<ILogger, Exception?> LogOverlapMessage =
        LoggerMessage.Define(LogLevel.Warning, new EventId(7004, nameof(CronSchedulerService)),
            "scheduler.sync_skipped overlap");

    private readonly ISyncCoordinator _coordinator;
    private readonly IOptionsMonitor<SynchronizationOptions> _syncOptions;
    private readonly ISyncState _syncState;
    private readonly ILogger<CronSchedulerService> _logger;

    public CronSchedulerService(
        ISyncCoordinator coordinator,
        IOptionsMonitor<SynchronizationOptions> syncOptions,
        ISyncState syncState,
        ILogger<CronSchedulerService> logger)
    {
        _coordinator = coordinator;
        _syncOptions = syncOptions;
        _syncState = syncState;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = GetDelayUntilNextOccurrence();
            LogNextRunMessage(_logger, delay, null);

            try
            {
                if (delay > TimeSpan.Zero)
                {
                    await Task.Delay(delay, stoppingToken).ConfigureAwait(false);
                }
            }
            catch (TaskCanceledException)
            {
                break;
            }

            if (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            await AttemptSyncAsync(stoppingToken).ConfigureAwait(false);
        }
    }

    [SuppressMessage("Design", "CA1031", Justification = "Scheduler must guard against unexpected exceptions to keep the background service alive.")]
    private async Task AttemptSyncAsync(CancellationToken stoppingToken)
    {
        if (!_syncState.TryMarkRunning())
        {
            LogOverlapMessage(_logger, null);
            return;
        }

        try
        {
            var result = await _coordinator.SynchronizeAsync(_syncOptions.CurrentValue.DryRun, stoppingToken).ConfigureAwait(false);
            var allSuccess = result.Secondaries.All(s => s.Status != TeleporterSyncStatus.Failed);
            if (allSuccess)
            {
                _syncState.MarkSuccess(DateTimeOffset.UtcNow);
            }
            else
            {
                _syncState.MarkFailure(DateTimeOffset.UtcNow);
            }
        }
        catch (Exception ex)
        {
            LogSyncFailedMessage(_logger, ex.Message, ex);
            _syncState.MarkFailure(DateTimeOffset.UtcNow);
        }
        finally
        {
            _syncState.MarkIdle();
        }
    }

    private TimeSpan GetDelayUntilNextOccurrence()
    {
        var options = _syncOptions.CurrentValue;
        if (!string.IsNullOrWhiteSpace(options.CronExpression))
        {
            try
            {
                var cron = CronExpression.Parse(options.CronExpression);
                var next = cron.GetNextOccurrence(DateTimeOffset.UtcNow, TimeZoneInfo.Utc);
                if (next.HasValue)
                {
                    var delay = next.Value - DateTimeOffset.UtcNow;
                    return delay > TimeSpan.Zero ? delay : TimeSpan.Zero;
                }
            }
            catch (CronFormatException ex)
            {
                LogInvalidCronMessage(_logger, options.CronExpression ?? string.Empty, ex);
            }
        }

        return options.Interval > TimeSpan.Zero ? options.Interval : TimeSpan.FromMinutes(5);
    }
}
