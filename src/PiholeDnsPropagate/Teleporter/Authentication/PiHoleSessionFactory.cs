using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Flurl;
using Flurl.Http;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using PiholeDnsPropagate.Teleporter;

namespace PiholeDnsPropagate.Teleporter.Authentication;

internal sealed class PiHoleSessionFactory : IPiHoleSessionFactory
{
    private readonly ILogger<PiHoleSessionFactory> _logger;
    private static readonly TimeSpan FallbackValidityWindow = TimeSpan.FromMinutes(5);

    public PiHoleSessionFactory(ILogger<PiHoleSessionFactory> logger)
    {
        _logger = logger;
    }

    private static readonly Action<ILogger, string, Exception?> LogAuthenticationFailure =
        LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(2001, nameof(PiHoleSessionFactory)),
            "Failed to authenticate against Pi-hole instance {InstanceName}.");

    private static readonly Action<ILogger, string, string?, Exception?> LogInvalidSession =
        LoggerMessage.Define<string, string?>(
            LogLevel.Error,
            new EventId(2002, nameof(PiHoleSessionFactory)),
            "Pi-hole instance {InstanceName} rejected credentials. Message: {Message}");

    public async Task<PiHoleSession> CreateSessionAsync(TeleporterClientOptions options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        var baseUrl = EnsureTrailingSlash(options.BaseUrl ?? throw new InvalidOperationException("Pi-hole BaseUrl not configured."));
        using var client = BuildClient(baseUrl, options.RequestTimeout);
        try
        {
            var authResponse = await client
                .Request("api", "auth")
                .PostJsonAsync(new
                {
                    password = options.Password ?? string.Empty
                }, cancellationToken: cancellationToken)
                .ReceiveJson<AuthResponse>()
                .ConfigureAwait(false);

            if (authResponse?.Session is null || !authResponse.Session.Valid || string.IsNullOrWhiteSpace(authResponse.Session.Sid))
            {
                LogInvalidSession(_logger, options.InstanceName, authResponse?.Session?.Message, null);
                throw new InvalidOperationException($"Pi-hole authentication failed for {options.InstanceName}.");
            }

            return new PiHoleSession
            {
                Sid = authResponse.Session.Sid,
                CsrfToken = authResponse.Session.Csrf,
                TotpEnabled = authResponse.Session.Totp,
                Validity = authResponse.Session.Validity > 0
                    ? TimeSpan.FromSeconds(authResponse.Session.Validity)
                    : FallbackValidityWindow,
                CreatedAt = DateTimeOffset.UtcNow
            };
        }
        catch (FlurlHttpException ex)
        {
            LogAuthenticationFailure(_logger, options.InstanceName, ex);
            throw;
        }
    }

    [SuppressMessage("Reliability", "CA2000", Justification = "FlurlClient disposes its inner HttpClient")]
    private static FlurlClient BuildClient(string baseUrl, TimeSpan timeout)
    {
        var handler = new HttpClientHandler();
        handler.CheckCertificateRevocationList = true;
        var httpClient = new HttpClient(handler)
        {
            Timeout = timeout > TimeSpan.Zero ? timeout : TimeSpan.FromSeconds(30)
        };

        return new FlurlClient(httpClient)
        {
            BaseUrl = baseUrl
        };
    }

    private static string EnsureTrailingSlash(Uri uri)
    {
        var text = uri.ToString();
        if (!text.EndsWith('/'))
        {
            text += "/";
        }

        return text;
    }

    private sealed class AuthResponse
    {
        public AuthSession? Session { get; init; }
        public double Took { get; init; }
    }

    private sealed class AuthSession
    {
        public bool Valid { get; init; }
        public bool Totp { get; init; }
        public string? Sid { get; init; }
        public string? Csrf { get; init; }
        public int Validity { get; init; }
        public string? Message { get; init; }
    }
}
