using System;
using FluentValidation;
using PiholeDnsPropagate.Options;

namespace PiholeDnsPropagate.Validation;

internal sealed class SynchronizationOptionsValidator : AbstractValidator<SynchronizationOptions>
{
    public SynchronizationOptionsValidator()
    {
        RuleFor(x => x)
            .Must(HaveIntervalOrCron)
            .WithMessage("Either SYNC_INTERVAL must be a positive TimeSpan or SYNC_CRON must be provided.");

        RuleFor(x => x.Interval)
            .GreaterThan(TimeSpan.Zero)
            .When(x => string.IsNullOrWhiteSpace(x.CronExpression))
            .WithMessage("SYNC_INTERVAL must be greater than zero when SYNC_CRON is not specified.");

        RuleFor(x => x.RequestTimeout)
            .GreaterThan(TimeSpan.Zero)
            .WithMessage("HTTP_TIMEOUT must be greater than zero.");
    }

    private static bool HaveIntervalOrCron(SynchronizationOptions options)
    {
        return (options.Interval > TimeSpan.Zero) || !string.IsNullOrWhiteSpace(options.CronExpression);
    }
}
