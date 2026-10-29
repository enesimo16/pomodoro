using FluentValidation;
using Focus.Domain.Entities;

namespace Focus.Application.Features.Session.Commands;

public class ExtendSessionCommandValidator : AbstractValidator<ExtendSessionCommand>
{
    public ExtendSessionCommandValidator()
    {
        RuleFor(x => x.ExtraMinutes)
            .InclusiveBetween(1, FocusSession.MaxExtensionMinutes)
            .WithMessage($"Uzatma süresi 1 ile {FocusSession.MaxExtensionMinutes} dakika arasında olmalıdır.");
    }
}
