using System;
using FluentValidation;
using PiholeDnsPropagate.Options;

namespace PiholeDnsPropagate.Validation;

internal sealed class PrimaryPiHoleOptionsValidator : AbstractValidator<PrimaryPiHoleOptions>
{
    public PrimaryPiHoleOptionsValidator()
    {
        RuleFor(x => x.BaseUrl)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .WithMessage("PRIMARY_PIHOLE_URL must be provided.")
            .Must(uri => uri is { IsAbsoluteUri: true })
            .WithMessage("PRIMARY_PIHOLE_URL must be an absolute URI.");

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("PRIMARY_PIHOLE_PASSWORD must be provided.");
    }
}
