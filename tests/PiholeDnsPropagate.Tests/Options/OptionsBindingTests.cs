using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using PiholeDnsPropagate.Extensions;
using PiholeDnsPropagate.Options;

namespace PiholeDnsPropagate.Tests.Options;

[TestFixture]
[SuppressMessage("MicrosoftDesign", "CA1515", Justification = "Test fixtures are public for NUnit discovery.")]
public class OptionsBindingTests
{
    [Test]
    public void BindsPrimaryAndSecondaryOptionsFromEnvironment()
    {
        var config = BuildConfiguration(new Dictionary<string, string?>
        {
            ["PRIMARY_PIHOLE_URL"] = "https://primary.local",
            ["PRIMARY_PIHOLE_PASSWORD"] = "secret",
            ["SECONDARY_PIHOLE_URLS"] = "https://node1.local,https://node2.local",
            ["SECONDARY_PIHOLE_PASSWORDS"] = "pass1,pass2",
            ["SECONDARY_PIHOLE_NAMES"] = "node1,node2",
            ["SYNC_INTERVAL"] = "00:10:00",
            ["SYNC_DRY_RUN"] = "true",
            ["LOG_LEVEL"] = "Debug",
            ["HEALTH_PORT"] = "9000",
            ["HTTP_TIMEOUT"] = "00:00:10"
        });

        using var provider = BuildServiceProvider(config);

        var primary = provider.GetRequiredService<IOptionsMonitor<PrimaryPiHoleOptions>>().CurrentValue;
        Assert.That(primary.BaseUrl, Is.EqualTo(new Uri("https://primary.local")));
        Assert.That(primary.Password, Is.EqualTo("secret"));

        var secondary = provider.GetRequiredService<IOptionsMonitor<SecondaryPiHoleOptions>>().CurrentValue;
        Assert.That(secondary.Nodes, Has.Count.EqualTo(2));
        Assert.That(secondary.Nodes[0].Name, Is.EqualTo("node1"));
        Assert.That(secondary.Nodes[0].BaseUrl, Is.EqualTo(new Uri("https://node1.local")));
        Assert.That(secondary.Nodes[0].Password, Is.EqualTo("pass1"));

        var sync = provider.GetRequiredService<IOptionsMonitor<SynchronizationOptions>>().CurrentValue;
        Assert.That(sync.Interval, Is.EqualTo(TimeSpan.FromMinutes(10)));
        Assert.That(sync.DryRun, Is.True);
        Assert.That(sync.RequestTimeout, Is.EqualTo(TimeSpan.FromSeconds(10)));

        var app = provider.GetRequiredService<IOptionsMonitor<ApplicationOptions>>().CurrentValue;
        Assert.That(app.LogLevel, Is.EqualTo("Debug"));
        Assert.That(app.HealthPort, Is.EqualTo(9000));
    }

    [Test]
    public void ThrowsForMismatchedSecondaryCredentials()
    {
        var config = BuildConfiguration(new Dictionary<string, string?>
        {
            ["SECONDARY_PIHOLE_URLS"] = "https://node1.local,https://node2.local",
            ["SECONDARY_PIHOLE_PASSWORDS"] = "pass1"
        });

        using var provider = BuildServiceProvider(config);

        Assert.That(
            () => provider.GetRequiredService<IOptionsMonitor<SecondaryPiHoleOptions>>().CurrentValue,
            Throws.Exception.TypeOf<OptionsValidationException>());
    }

    private static IConfiguration BuildConfiguration(Dictionary<string, string?> values)
    {
        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(values);
        return builder.Build();
    }

    private static ServiceProvider BuildServiceProvider(IConfiguration configuration)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplicationOptions(configuration);
        return services.BuildServiceProvider();
    }
}
