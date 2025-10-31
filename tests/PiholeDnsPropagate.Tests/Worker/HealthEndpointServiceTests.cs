using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using PiholeDnsPropagate.Options;
using PiholeDnsPropagate.Worker.Services;
using PiholeDnsPropagate.Tests.Teleporter.Fixtures;

namespace PiholeDnsPropagate.Tests.Worker;

[TestFixture]
[System.Diagnostics.CodeAnalysis.SuppressMessage("MicrosoftDesign", "CA1515", Justification = "NUnit requires public fixtures for discovery.")]
public sealed class HealthEndpointServiceTests
{
    [Test]
    public async Task HealthEndpointReportsSyncState()
    {
        // Arrange
        var syncState = new SyncState();
        syncState.TryMarkRunning();
        syncState.MarkSuccess(DateTimeOffset.UtcNow);

        var port = GetFreePort();
        var options = new TestOptionsMonitor<ApplicationOptions>(new ApplicationOptions { HealthPort = port });
        using var service = new HealthEndpointService(syncState, options, NullLogger<HealthEndpointService>.Instance);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await service.StartAsync(cts.Token).ConfigureAwait(false);

        using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };

        // Act
        using var response = await WaitForHealthAsync(httpClient, port, cts.Token).ConfigureAwait(false);
        var payload = await response.Content.ReadAsStringAsync(cts.Token).ConfigureAwait(false);

        // Assert
        Assert.That(response.IsSuccessStatusCode, Is.True);

        using var json = JsonDocument.Parse(payload);
        var root = json.RootElement;
        Assert.That(root.GetProperty("status").GetString(), Is.EqualTo("Success"));
        Assert.That(root.GetProperty("running").GetBoolean(), Is.False);
        Assert.That(root.TryGetProperty("lastSuccess", out var lastSuccess), Is.True);
        Assert.That(lastSuccess.GetString(), Is.Not.Null);

        await service.StopAsync(CancellationToken.None).ConfigureAwait(false);
    }

    [Test]
    public async Task HealthEndpointReturnsNotFoundForUnknownRoute()
    {
        // Arrange
        var syncState = new SyncState();
        var port = GetFreePort();
        var options = new TestOptionsMonitor<ApplicationOptions>(new ApplicationOptions { HealthPort = port });
        using var service = new HealthEndpointService(syncState, options, NullLogger<HealthEndpointService>.Instance);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await service.StartAsync(cts.Token).ConfigureAwait(false);

        using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };

        // Act
        using var response = await httpClient.GetAsync(new Uri($"http://127.0.0.1:{port}/metrics"), cts.Token).ConfigureAwait(false);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.NotFound));

        await service.StopAsync(CancellationToken.None).ConfigureAwait(false);
    }

    [SuppressMessage("Reliability", "CA2000", Justification = "TcpListener resources are released via Stop().")]
    private static int GetFreePort()
    {
        var listener = new TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();
        var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    [SuppressMessage("Design", "CA1031", Justification = "Polling tolerates transient listener startup failures.")]
    private static async Task<HttpResponseMessage> WaitForHealthAsync(HttpClient httpClient, int port, CancellationToken cancellationToken)
    {
        var endpoint = new Uri($"http://127.0.0.1:{port}/healthz");
        var attempts = 0;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var response = await httpClient.GetAsync(endpoint, cancellationToken).ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                {
                    return response;
                }

                response.Dispose();
            }
            catch
            {
                // Listener might not be ready yet; keep retrying until timeout is reached.
            }

            attempts++;
            await Task.Delay(TimeSpan.FromMilliseconds(200 * Math.Min(attempts, 5)), cancellationToken).ConfigureAwait(false);
        }
    }
}
