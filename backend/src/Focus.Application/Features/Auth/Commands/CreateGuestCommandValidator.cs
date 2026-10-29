using FluentValidation;

namespace Focus.Application.Features.Auth.Commands;

public class CreateGuestCommandValidator : AbstractValidator<CreateGuestCommand>
{
    public const int MaxDisplayNameLength = 100;
    public const int MaxLocaleLength = 20;
    public const int MaxTimeZoneIdLength = 50;

    public CreateGuestCommandValidator()
    {
        RuleFor(x => x.DisplayName)
            .MaximumLength(MaxDisplayNameLength)
            .WithMessage($"Görünen ad en fazla {MaxDisplayNameLength} karakter olabilir.");

        RuleFor(x => x.Locale)
            .MaximumLength(MaxLocaleLength)
            .WithMessage($"Dil kodu en fazla {MaxLocaleLength} karakter olabilir.");

        RuleFor(x => x.TimeZoneId)
            .MaximumLength(MaxTimeZoneIdLength)
            .WithMessage($"Saat dilimi en fazla {MaxTimeZoneIdLength} karakter olabilir.")
            .Must(IsKnownTimeZone)
            .When(x => !string.IsNullOrEmpty(x.TimeZoneId))
            .WithMessage("Geçersiz saat dilimi.");
    }

    private static bool IsKnownTimeZone(string? timeZoneId)
    {
        return TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId!, out _);
    }
}
