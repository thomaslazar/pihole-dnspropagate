using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PiholeDnsPropagate.Options;
using PiholeDnsPropagate.Security;
using PiholeDnsPropagate.Validation;

namespace PiholeDnsPropagate.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<PrimaryPiHoleOptions>()
            .Configure(options => BindPrimary(configuration, options))
            .PostConfigure(options =>
            {
                ValidateOptions(options, new PrimaryPiHoleOptionsValidator(), nameof(PrimaryPiHoleOptions));
                options.PasswordHash = PasswordHasher.ComputeSha256(options.Password);
            });

        services.AddOptions<SecondaryPiHoleOptions>()
            .Configure(options => BindSecondary(configuration, options))
            .PostConfigure(options =>
            {
                ValidateOptions(options, new SecondaryPiHoleOptionsValidator(), nameof(SecondaryPiHoleOptions));
                foreach (var node in options.Nodes)
                {
                    node.PasswordHash = PasswordHasher.ComputeSha256(node.Password);
                }
            });

        services.AddOptions<SynchronizationOptions>()
            .Configure(options => BindSynchronization(configuration, options))
            .PostConfigure(options =>
                ValidateOptions(options, new SynchronizationOptionsValidator(), nameof(SynchronizationOptions)));

        services.AddOptions<ApplicationOptions>()
            .Configure(options => BindApplication(configuration, options))
            .PostConfigure(options =>
                ValidateOptions(options, new ApplicationOptionsValidator(), nameof(ApplicationOptions)));

        return services;
    }

    private static void BindPrimary(IConfiguration configuration, PrimaryPiHoleOptions options)
    {
        options.BaseUrl = TryCreateUri(configuration["PRIMARY_PIHOLE_URL"]);
        options.Password = configuration["PRIMARY_PIHOLE_PASSWORD"] ?? string.Empty;
    }

    private static void BindSecondary(IConfiguration configuration, SecondaryPiHoleOptions options)
    {
        var urls = SplitValues(configuration["SECONDARY_PIHOLE_URLS"]).ToList();
        var passwords = SplitValues(configuration["SECONDARY_PIHOLE_PASSWORDS"]).ToList();
        var names = SplitValues(configuration["SECONDARY_PIHOLE_NAMES"]).ToList();

        options.Nodes.Clear();

        var count = Math.Max(urls.Count, Math.Max(passwords.Count, names.Count));
        for (var index = 0; index < count; index++)
        {
            var url = index < urls.Count ? urls[index] : string.Empty;
            var password = index < passwords.Count ? passwords[index] : string.Empty;
            var name = index < names.Count ? names[index] : CreateDefaultName(url, index);

            options.Nodes.Add(new SecondaryPiHoleNodeOptions
            {
                Name = name,
                BaseUrl = TryCreateUri(url),
                Password = password
            });
        }
    }

    private static void BindSynchronization(IConfiguration configuration, SynchronizationOptions options)
    {
        options.CronExpression = configuration["SYNC_CRON"];
        options.DryRun = GetBool(configuration, "SYNC_DRY_RUN");
        options.Interval = ParseTimeSpan(configuration["SYNC_INTERVAL"], TimeSpan.FromMinutes(5));
        options.RequestTimeout = ParseTimeSpan(configuration["HTTP_TIMEOUT"], TimeSpan.FromSeconds(30));
    }

    private static void BindApplication(IConfiguration configuration, ApplicationOptions options)
    {
        options.LogLevel = configuration["LOG_LEVEL"] ?? "Information";
        options.HealthPort = ParseInt(configuration["HEALTH_PORT"], 8080);
    }

    private static void ValidateOptions<TOptions>(TOptions options, IValidator<TOptions> validator, string name)
        where TOptions : class
    {
        var result = validator.Validate(options);
        if (!result.IsValid)
        {
            throw new OptionsValidationException(name, typeof(TOptions), result.Errors.Select(e => e.ErrorMessage).ToArray());
        }
    }

    private static IEnumerable<string> SplitValues(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Enumerable.Empty<string>();
        }

        return value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static Uri? TryCreateUri(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return Uri.TryCreate(value, UriKind.Absolute, out var uri) ? uri : null;
    }

    private static TimeSpan ParseTimeSpan(string? value, TimeSpan defaultValue)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        return TimeSpan.TryParse(value, out var result) ? result : defaultValue;
    }

    private static int ParseInt(string? value, int defaultValue)
    {
        if (int.TryParse(value, out var parsed))
        {
            return parsed;
        }

        return defaultValue;
    }

    private static bool GetBool(IConfiguration configuration, string key, bool defaultValue = false)
    {
        var value = configuration[key];
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        return value.Equals("1", StringComparison.OrdinalIgnoreCase)
            || value.Equals("true", StringComparison.OrdinalIgnoreCase);
    }

    private static string CreateDefaultName(string url, int index)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri) && !string.IsNullOrWhiteSpace(uri.Host))
        {
            return uri.Host;
        }

        return $"secondary-{index + 1}";
    }
}
