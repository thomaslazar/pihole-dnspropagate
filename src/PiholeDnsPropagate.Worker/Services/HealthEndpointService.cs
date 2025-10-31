using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PiholeDnsPropagate.Options;

namespace PiholeDnsPropagate.Worker.Services;

internal sealed class HealthEndpointService : BackgroundService
{
    private static readonly Action<ILogger, Exception?> LogUnsupportedMessage =
        LoggerMessage.Define(LogLevel.Warning, new EventId(7201, nameof(HealthEndpointService)),
            "health.listener.unsupported");

    private static readonly Action<ILogger, int, Exception?> LogStartedMessage =
        LoggerMessage.Define<int>(LogLevel.Information, new EventId(7202, nameof(HealthEndpointService)),
            "health.listener.started {Port}");

    private static readonly Action<ILogger, int, Exception?> LogStartFailedMessage =
        LoggerMessage.Define<int>(LogLevel.Error, new EventId(7203, nameof(HealthEndpointService)),
            "health.listener.start_failed {Port}");

    private static readonly Action<ILogger, Exception?> LogContextErrorMessage =
        LoggerMessage.Define(LogLevel.Error, new EventId(7204, nameof(HealthEndpointService)),
            "health.listener.context_error");

    private static readonly Action<ILogger, Exception?> LogRequestFailedMessage =
        LoggerMessage.Define(LogLevel.Error, new EventId(7205, nameof(HealthEndpointService)),
            "health.listener.request_failed");

    private readonly ISyncState _syncState;
    private readonly IOptionsMonitor<ApplicationOptions> _appOptions;
    private readonly ILogger<HealthEndpointService> _logger;
    private HttpListener? _listener;

    public HealthEndpointService(
        ISyncState syncState,
        IOptionsMonitor<ApplicationOptions> appOptions,
        ILogger<HealthEndpointService> logger)
    {
        _syncState = syncState;
        _appOptions = appOptions;
        _logger = logger;
    }

    [SuppressMessage("Design", "CA1031", Justification = "Health listener must remain available even when incoming requests throw unexpected exceptions.")]
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!HttpListener.IsSupported)
        {
            LogUnsupportedMessage(_logger, null);
            return;
        }

        var port = _appOptions.CurrentValue.HealthPort;
        _listener = new HttpListener
        {
            Prefixes = { $"http://*:{port}/" }
        };

        try
        {
            _listener.Start();
            LogStartedMessage(_logger, port, null);
        }
        catch (HttpListenerException ex)
        {
            LogStartFailedMessage(_logger, port, ex);
            return;
        }

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                HttpListenerContext context;
                try
                {
                    context = await _listener.GetContextAsync().WaitAsync(stoppingToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    LogContextErrorMessage(_logger, ex);
                    continue;
                }

                _ = Task.Run(() => HandleRequestAsync(context, stoppingToken), stoppingToken);
            }
        }
        finally
        {
            if (_listener is not null)
            {
                _listener.Stop();
                _listener.Close();
            }
        }
    }

    [SuppressMessage("Design", "CA1031", Justification = "Health endpoint must convert unexpected errors into HTTP 500 responses instead of crashing the host.")]
    private async Task HandleRequestAsync(HttpListenerContext context, CancellationToken cancellationToken)
    {
        try
        {
            var requestPath = context.Request.Url?.AbsolutePath ?? string.Empty;
            if (!string.Equals(requestPath, "/healthz", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                context.Response.Close();
                return;
            }

            var payload = JsonSerializer.Serialize(new
            {
                status = _syncState.CurrentStatus.ToString(),
                running = _syncState.IsRunning,
                lastSuccess = _syncState.LastSuccess,
                lastFailure = _syncState.LastFailure
            });

            var buffer = Encoding.UTF8.GetBytes(payload);
            context.Response.StatusCode = (int)HttpStatusCode.OK;
            context.Response.ContentType = "application/json";
            context.Response.ContentEncoding = Encoding.UTF8;
            context.Response.ContentLength64 = buffer.Length;
            await context.Response.OutputStream.WriteAsync(buffer.AsMemory(0, buffer.Length), cancellationToken).ConfigureAwait(false);
            context.Response.Close();
        }
        catch (Exception ex)
        {
            LogRequestFailedMessage(_logger, ex);
            try
            {
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                context.Response.Close();
            }
            catch
            {
                // ignore secondary failures
            }
        }
    }

    public override void Dispose()
    {
        _listener?.Close();
        _listener = null;
        base.Dispose();
    }
}
