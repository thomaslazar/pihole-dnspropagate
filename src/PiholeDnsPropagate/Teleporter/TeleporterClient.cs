using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Flurl;
using Flurl.Http;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using PiholeDnsPropagate.Teleporter.Authentication;

namespace PiholeDnsPropagate.Teleporter;

internal sealed class TeleporterClient : ITeleporterClient, IDisposable, IAsyncDisposable
{
    private readonly TeleporterClientOptions _options;
    private readonly IPiHoleSessionFactory _sessionFactory;
    private readonly ILogger<TeleporterClient> _logger;
    private readonly AsyncRetryPolicy _retryPolicy;
    private readonly FlurlClient _client;
    private readonly string _baseUrl;

    private PiHoleSession? _session;
    private bool _disposed;

    private static readonly Action<ILogger, string, int, TimeSpan, Exception?> LogRetryMessage =
        LoggerMessage.Define<string, int, TimeSpan>(
            LogLevel.Warning,
            new EventId(1001, nameof(TeleporterClient)),
            "Retrying Teleporter request for {Instance} (attempt {Attempt}, delay {Delay}).");

    private static readonly Action<ILogger, string, Exception?> LogSessionInvalidateFailure =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(1002, nameof(TeleporterClient)),
            "Failed to invalidate Pi-hole session for {Instance}.");

    [SuppressMessage("Reliability", "CA2000", Justification = "FlurlClient disposes HttpClient")] 
    public TeleporterClient(
        TeleporterClientOptions options,
        IPiHoleSessionFactory sessionFactory,
        ILogger<TeleporterClient> logger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _sessionFactory = sessionFactory ?? throw new ArgumentNullException(nameof(sessionFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _baseUrl = EnsureTrailingSlash(_options.BaseUrl);
        var handler = new HttpClientHandler
        {
            UseCookies = false,
            CheckCertificateRevocationList = true
        };

        var httpClient = new HttpClient(handler, disposeHandler: true)
        {
            Timeout = _options.RequestTimeout > TimeSpan.Zero ? _options.RequestTimeout : TimeSpan.FromSeconds(30)
        };

        _client = new FlurlClient(httpClient)
        {
            BaseUrl = _baseUrl
        };

        _retryPolicy = Policy
            .Handle<FlurlHttpException>(ShouldRetry)
            .WaitAndRetryAsync(3, attempt => TimeSpan.FromMilliseconds(200 * attempt), (exception, delay, retryCount, _) =>
            {
                LogRetryMessage(_logger, _options.InstanceName, retryCount, delay, exception);
            });
    }

    public async Task<byte[]> DownloadArchiveAsync(CancellationToken cancellationToken = default)
    {
        await EnsureSessionAsync(cancellationToken).ConfigureAwait(false);

        return await _retryPolicy.ExecuteAsync(async ct =>
        {
            try
            {
                using var response = await CreateRequest("api", "teleporter")
                    .WithHeader("Accept", "application/zip")
                    .GetAsync(cancellationToken: ct)
                    .ConfigureAwait(false);

                response.ResponseMessage.EnsureSuccessStatusCode();

                return await response.GetBytesAsync().ConfigureAwait(false);
            }
            catch (FlurlHttpException ex) when (IsUnauthorized(ex))
            {
                await RefreshSessionAsync(ct).ConfigureAwait(false);
                throw;
            }
        }, cancellationToken).ConfigureAwait(false);
    }

    public async Task UploadArchiveAsync(byte[] archiveContent, CancellationToken cancellationToken = default)
    {
        if (archiveContent is null || archiveContent.Length == 0)
        {
            throw new ArgumentException("Archive content must be provided.", nameof(archiveContent));
        }

        await EnsureSessionAsync(cancellationToken).ConfigureAwait(false);

        await _retryPolicy.ExecuteAsync(async ct =>
        {
            try
            {
                using var stream = new MemoryStream(archiveContent, writable: false);

                using var response = await CreateRequest("api", "teleporter")
                    .PostMultipartAsync(content =>
                    {
                        content.AddFile("file", stream, "teleporter.zip");
                    }, cancellationToken: ct)
                    .ConfigureAwait(false);

                response.ResponseMessage.EnsureSuccessStatusCode();
            }
            catch (FlurlHttpException ex) when (IsUnauthorized(ex))
            {
                await RefreshSessionAsync(ct).ConfigureAwait(false);
                throw;
            }
        }, cancellationToken).ConfigureAwait(false);
    }

    private async Task EnsureSessionAsync(CancellationToken cancellationToken)
    {
        if (_session == null)
        {
            _session = await _sessionFactory.CreateSessionAsync(_options, cancellationToken).ConfigureAwait(false);
            return;
        }

        if (SessionExpired(_session))
        {
            await InvalidateSessionAsync(cancellationToken).ConfigureAwait(false);
            _session = await _sessionFactory.CreateSessionAsync(_options, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task RefreshSessionAsync(CancellationToken cancellationToken)
    {
        if (_session != null)
        {
            await InvalidateSessionAsync(cancellationToken).ConfigureAwait(false);
        }

        _session = await _sessionFactory.CreateSessionAsync(_options, cancellationToken).ConfigureAwait(false);
    }

    private static bool SessionExpired(PiHoleSession session)
        => string.IsNullOrWhiteSpace(session.Sid) || session.IsExpired(DateTimeOffset.UtcNow);

    private IFlurlRequest CreateRequest(params string[] segments)
    {
        var request = _client.Request(segments);

        if (_session != null)
        {
            if (!string.IsNullOrWhiteSpace(_session.Sid))
            {
                request = request.WithHeader("X-FTL-SID", _session.Sid);
            }

            if (!string.IsNullOrWhiteSpace(_session.CsrfToken))
            {
                request = request.WithHeader("X-FTL-CSRF", _session.CsrfToken);
            }
        }

        return request;
    }

    private static bool ShouldRetry(FlurlHttpException exception)
    {
        var statusCode = exception.StatusCode;
        if (!statusCode.HasValue)
        {
            return false;
        }

        return statusCode.Value is (int)HttpStatusCode.RequestTimeout
            or (int)HttpStatusCode.Unauthorized
            or (int)HttpStatusCode.Forbidden
            || statusCode.Value >= 500;
    }

    private static bool IsUnauthorized(FlurlHttpException ex)
        => ex.StatusCode is (int)HttpStatusCode.Unauthorized or (int)HttpStatusCode.Forbidden;

    private static string EnsureTrailingSlash(Uri? uri)
    {
        if (uri == null)
        {
            ArgumentNullException.ThrowIfNull(uri);
        }

        var text = uri.ToString();
        if (!text.EndsWith('/'))
        {
            text += "/";
        }

        return text;
    }

    public void Dispose()
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult();
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        try
        {
            await InvalidateSessionAsync(CancellationToken.None).ConfigureAwait(false);
        }
        finally
        {
            _client.Dispose();
        }

        GC.SuppressFinalize(this);
    }

    [SuppressMessage("Design", "CA1031", Justification = "Session invalidation should not crash the worker; unexpected failures are logged.")]
    private async Task InvalidateSessionAsync(CancellationToken cancellationToken)
    {
        var session = _session;
        if (session is null || string.IsNullOrWhiteSpace(session.Sid))
        {
            return;
        }

        try
        {
            using var response = await CreateRequest("api", "auth")
                .DeleteAsync(cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            // 2xx responses simply succeed. No additional handling required.
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (FlurlHttpException ex) when (ex.StatusCode is (int)HttpStatusCode.NotFound or (int)HttpStatusCode.Unauthorized)
        {
            // Session already gone; nothing to log.
        }
        catch (Exception ex)
        {
            LogSessionInvalidateFailure(_logger, _options.InstanceName, ex);
        }
        finally
        {
            _session = null;
        }
    }
}
