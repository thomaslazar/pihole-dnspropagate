using System;
using System.Linq;
using FluentValidation;
using PiholeDnsPropagate.Options;

namespace PiholeDnsPropagate.Validation;

internal sealed class SecondaryPiHoleOptionsValidator : AbstractValidator<SecondaryPiHoleOptions>
{
    public SecondaryPiHoleOptionsValidator()
    {
        RuleForEach(x => x.Nodes)
            .SetValidator(new SecondaryPiHoleNodeValidator());

        RuleFor(x => x.Nodes)
            .Must(nodes => nodes.Select(n => n.BaseUrl?.ToString()).Where(u => u != null).Distinct(StringComparer.OrdinalIgnoreCase).Count() == nodes.Count)
            .WithMessage("SECONDARY_PIHOLE_URLS contains duplicate entries.");
    }

    private sealed class SecondaryPiHoleNodeValidator : AbstractValidator<SecondaryPiHoleNodeOptions>
    {
        public SecondaryPiHoleNodeValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Each secondary Pi-hole must have a name.");

            RuleFor(x => x.BaseUrl)
                .Cascade(CascadeMode.Stop)
                .NotNull()
                .WithMessage("SECONDARY_PIHOLE_URLS must include valid URIs.")
                .Must(uri => uri is { IsAbsoluteUri: true })
                .WithMessage("Secondary Pi-hole URLs must be absolute URIs.");

            RuleFor(x => x.Password)
                .NotEmpty()
                .WithMessage("SECONDARY_PIHOLE_PASSWORDS must provide a password for each URL.");
        }
    }
}
