using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace PiholeDnsPropagate.Tests.Integration;

internal sealed class PiHoleCluster : IAsyncDisposable
{
    private const string Image = "pihole/pihole:2024.07.0";
    private const string AdminPassword = "PiholeSync#2025";

    private readonly ITestcontainersContainer _primary;
    private readonly ITestcontainersContainer _secondary;

    private PiHoleCluster(ITestcontainersContainer primary, ITestcontainersContainer secondary)
    {
        _primary = primary;
        _secondary = secondary;
    }

    public Uri PrimaryBaseUri => BuildBaseUri(_primary);
    public Uri SecondaryBaseUri => BuildBaseUri(_secondary);

    [SuppressMessage("Performance", "CA1822", Justification = "Fixture API is consumed as instance property for readability.")]
    public string Password => AdminPassword;

    [SuppressMessage("Reliability", "CA2000", Justification = "Containers are disposed via PiHoleCluster.DisposeAsync().")]
    public static async Task<PiHoleCluster> StartAsync(CancellationToken cancellationToken)
    {
        var primary = CreatePiHoleContainer("primary");
        var secondary = CreatePiHoleContainer("secondary");

        try
        {
            await primary.StartAsync(cancellationToken).ConfigureAwait(false);
            await secondary.StartAsync(cancellationToken).ConfigureAwait(false);

            await WaitForReadyAsync(primary, cancellationToken).ConfigureAwait(false);
            await WaitForReadyAsync(secondary, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            await DisposeContainerAsync(primary).ConfigureAwait(false);
            await DisposeContainerAsync(secondary).ConfigureAwait(false);
            throw;
        }

        return new PiHoleCluster(primary, secondary);
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeContainerAsync(_primary).ConfigureAwait(false);
        await DisposeContainerAsync(_secondary).ConfigureAwait(false);
    }

    private static TestcontainersContainer CreatePiHoleContainer(string suffix)
    {
        var name = $"pihole-{suffix}-{Guid.NewGuid():N}";

        return new TestcontainersBuilder<TestcontainersContainer>()
            .WithImage(Image)
            .WithName(name)
            .WithCleanUp(true)
            .WithPrivileged(true)
            .WithEnvironment("TZ", "UTC")
            .WithEnvironment("WEBPASSWORD", AdminPassword)
            .WithEnvironment("DNSMASQ_LISTENING", "local")
            .WithEnvironment("FTLCONF_LOCAL_IPV4", "0.0.0.0")
            .WithEnvironment("SKIPGRAVITY_ON_BOOT", "1")
            .WithPortBinding(0, 80)
            .Build();
    }

    private static Uri BuildBaseUri(ITestcontainersContainer container)
    {
        var port = container.GetMappedPublicPort(80);
        return new UriBuilder(Uri.UriSchemeHttp, "localhost", port).Uri;
    }

    [SuppressMessage("Design", "CA1031", Justification = "Startup polling deliberately tolerates transient HTTP failures.")]
    private static async Task WaitForReadyAsync(ITestcontainersContainer container, CancellationToken cancellationToken)
    {
        using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        var baseUri = BuildBaseUri(container);
        var deadline = DateTimeOffset.UtcNow.AddMinutes(4);

        while (DateTimeOffset.UtcNow <= deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                using var uiResponse = await httpClient.GetAsync(new Uri(baseUri, "/admin/index.php"), cancellationToken).ConfigureAwait(false);
                if (!uiResponse.IsSuccessStatusCode)
                {
                    uiResponse.Dispose();
                    throw new HttpRequestException($"Admin UI returned status {(int)uiResponse.StatusCode}");
                }

                using var apiResponse = await httpClient.GetAsync(new Uri(baseUri, "/api/info/version"), cancellationToken).ConfigureAwait(false);
                if (apiResponse.IsSuccessStatusCode)
                {
                    return;
                }

                apiResponse.Dispose();
            }
            catch
            {
                // Ignore transient startup failures while the container initializes.
            }

            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken).ConfigureAwait(false);
        }

        throw new TimeoutException($"Pi-hole container at {baseUri} did not become ready in time.");
    }

    [SuppressMessage("Design", "CA1031", Justification = "Container shutdown should not throw during cleanup.")]
    private static async Task DisposeContainerAsync(ITestcontainersContainer container)
    {
        try
        {
            await container.StopAsync().ConfigureAwait(false);
        }
        catch
        {
            // Ignore shutdown errors.
        }

        await container.DisposeAsync().ConfigureAwait(false);
    }
}
