using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Flurl.Http.Testing;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using PiholeDnsPropagate.Options;
using PiholeDnsPropagate.Teleporter;
using PiholeDnsPropagate.Teleporter.Authentication;

namespace PiholeDnsPropagate.Tests.Teleporter;

[TestFixture]
[System.Diagnostics.CodeAnalysis.SuppressMessage("MicrosoftDesign", "CA1515", Justification = "Test fixture must be public for NUnit discovery.")]
public class TeleporterClientTests
{
    [Test]
    public async Task DownloadArchiveAsyncReturnsPayload()
    {
        // Arrange
        using var httpTest = new HttpTest();
        httpTest.RespondWithJson(BuildAuthResponse("sid-token", "csrf-token"));
        httpTest.RespondWith("payload", 200, new Dictionary<string, string> { ["Content-Type"] = "application/zip" });

        using var client = CreateClient();

        // Act
        var bytes = await client.DownloadArchiveAsync().ConfigureAwait(false);

        // Assert
        Assert.That(bytes, Is.EqualTo(Encoding.UTF8.GetBytes("payload")));
        httpTest.ShouldHaveCalled("*/api/auth").WithVerb(HttpMethod.Post);
        httpTest.ShouldHaveCalled("*/api/teleporter")
            .WithVerb(HttpMethod.Get)
            .WithHeader("X-FTL-SID", "sid-token");
    }

    [Test]
    public void DownloadArchiveAsyncRaisesOnUnauthorized()
    {
        // Arrange
        using var httpTest = new HttpTest();
        httpTest.RespondWithJson(BuildAuthResponse("sid-token", "csrf-token"));
        httpTest.RespondWith(status: 401);
        httpTest.RespondWithJson(BuildAuthResponse("sid-token", "csrf-token"));
        httpTest.RespondWith("payload", 200, new Dictionary<string, string> { ["Content-Type"] = "application/zip" });

        using var client = CreateClient();

        // Act / Assert
        Assert.DoesNotThrowAsync(async () => await client.DownloadArchiveAsync().ConfigureAwait(false));
        httpTest.ShouldHaveCalled("*/api/auth").WithVerb(HttpMethod.Post).Times(2);
        httpTest.ShouldHaveCalled("*/api/teleporter").WithVerb(HttpMethod.Get).Times(2);
    }

    [Test]
    public async Task UploadArchiveAsyncSucceeds()
    {
        // Arrange
        using var httpTest = new HttpTest();
        httpTest.RespondWithJson(BuildAuthResponse("sid-token", "csrf-token"));
        httpTest.RespondWith("", 200); // upload response

        using var client = CreateClient();

        // Act
        await client.UploadArchiveAsync(new byte[] { 5, 6, 7 }).ConfigureAwait(false);

        // Assert
        httpTest.ShouldHaveCalled("*/api/teleporter")
            .WithVerb(HttpMethod.Post)
            .WithHeader("X-FTL-SID", "sid-token")
            .Times(1);
    }

    private static TeleporterClient CreateClient()
    {
        var options = new TeleporterClientOptions
        {
            InstanceName = "test",
            BaseUrl = new Uri("https://pi.local/"),
            Password = "secret"
        };

        var sessionFactory = new PiHoleSessionFactory(new NullLogger<PiHoleSessionFactory>());
        var logger = new NullLogger<TeleporterClient>();

        return new TeleporterClient(options, sessionFactory, logger);
    }

    private static object BuildAuthResponse(string sid, string? csrfToken)
        => new
        {
            session = new
            {
                valid = true,
                totp = false,
                sid,
                csrf = csrfToken,
                validity = 300,
                message = (string?)null
            },
            took = 0.01
        };
}
