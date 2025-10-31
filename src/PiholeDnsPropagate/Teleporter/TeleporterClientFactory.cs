using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PiholeDnsPropagate.Options;
using PiholeDnsPropagate.Teleporter.Authentication;

namespace PiholeDnsPropagate.Teleporter;

internal sealed class TeleporterClientFactory : ITeleporterClientFactory
{
    private readonly IPiHoleSessionFactory _sessionFactory;
    private readonly IOptionsMonitor<SynchronizationOptions> _syncOptions;
    private readonly ILogger<TeleporterClient> _teleporterLogger;

    public TeleporterClientFactory(
        IPiHoleSessionFactory sessionFactory,
        IOptionsMonitor<SynchronizationOptions> syncOptions,
        ILogger<TeleporterClient> teleporterLogger)
    {
        _sessionFactory = sessionFactory;
        _syncOptions = syncOptions;
        _teleporterLogger = teleporterLogger;
    }

    public ITeleporterClient CreateForPrimary(PrimaryPiHoleOptions options)
    {
        if (options.BaseUrl is null)
        {
            throw new InvalidOperationException("Primary Pi-hole BaseUrl must be configured.");
        }

        var clientOptions = new TeleporterClientOptions
        {
            InstanceName = "primary",
            BaseUrl = options.BaseUrl!,
            Password = options.Password,
            RequestTimeout = _syncOptions.CurrentValue.RequestTimeout
        };

        return new TeleporterClient(clientOptions, _sessionFactory, _teleporterLogger);
    }

    public ITeleporterClient CreateForSecondary(SecondaryPiHoleNodeOptions node)
    {
        if (node.BaseUrl is null)
        {
            throw new InvalidOperationException($"Secondary Pi-hole '{node.Name}' is missing BaseUrl.");
        }

        var clientOptions = new TeleporterClientOptions
        {
            InstanceName = node.Name,
            BaseUrl = node.BaseUrl!,
            Password = node.Password,
            RequestTimeout = _syncOptions.CurrentValue.RequestTimeout
        };

        return new TeleporterClient(clientOptions, _sessionFactory, _teleporterLogger);
    }
}
