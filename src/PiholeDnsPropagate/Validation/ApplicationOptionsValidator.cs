using System;
using FluentValidation;
using PiholeDnsPropagate.Options;

namespace PiholeDnsPropagate.Validation;

internal sealed class ApplicationOptionsValidator : AbstractValidator<ApplicationOptions>
{
    public ApplicationOptionsValidator()
    {
        RuleFor(x => x.LogLevel)
            .NotEmpty()
            .WithMessage("LOG_LEVEL must be provided.");

        RuleFor(x => x.HealthPort)
            .InclusiveBetween(1, 65535)
            .WithMessage("HEALTH_PORT must be between 1 and 65535.");
    }
}
