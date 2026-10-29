using FluentValidation;
using Focus.Domain.Entities;

namespace Focus.Application.Features.Session.Commands;

public class StartSessionCommandValidator : AbstractValidator<StartSessionCommand>
{
    public const int MaxIdentifierLength = 100;

    public StartSessionCommandValidator()
    {
        RuleFor(x => x.Kind)
            .IsInEnum().WithMessage("Geçersiz seans türü.");

        RuleFor(x => x.FocusMinutes)
            .InclusiveBetween(FocusSession.MinMinutes, FocusSession.MaxFocusMinutes)
            .WithMessage($"Odak süresi {FocusSession.MinMinutes} ile {FocusSession.MaxFocusMinutes} dakika arasında olmalıdır.");

        RuleFor(x => x.BreakMinutes)
            .InclusiveBetween(FocusSession.MinMinutes, FocusSession.MaxBreakMinutes)
            .WithMessage($"Mola süresi {FocusSession.MinMinutes} ile {FocusSession.MaxBreakMinutes} dakika arasında olmalıdır.");

        RuleFor(x => x.TargetRounds)
            .InclusiveBetween(1, FocusSession.MaxRounds)
            .WithMessage($"Tur sayısı 1 ile {FocusSession.MaxRounds} arasında olmalıdır.");

        RuleFor(x => x.CurrentRound)
            .InclusiveBetween(1, FocusSession.MaxRounds)
            .WithMessage($"Geçerli tur 1 ile {FocusSession.MaxRounds} arasında olmalıdır.")
            .LessThanOrEqualTo(x => x.TargetRounds)
            .WithMessage("Geçerli tur, hedef tur sayısını aşamaz.");

        RuleFor(x => x.ThemeId)
            .MaximumLength(MaxIdentifierLength).WithMessage("Tema kimliği çok uzun.");

        RuleFor(x => x.MixPresetId)
            .MaximumLength(MaxIdentifierLength).WithMessage("Karışım kimliği çok uzun.");
    }
}
