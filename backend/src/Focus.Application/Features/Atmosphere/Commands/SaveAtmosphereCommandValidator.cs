using FluentValidation;

namespace Focus.Application.Features.Atmosphere.Commands;

public class SaveAtmosphereCommandValidator : AbstractValidator<SaveAtmosphereCommand>
{
    public const int MaxPromptLength = 1000;

    public SaveAtmosphereCommandValidator()
    {
        RuleFor(x => x.Atmosphere).NotNull().WithMessage("Kaydedilecek atmosfer nesnesi gereklidir.");

        When(x => x.Atmosphere != null, () =>
        {
            RuleFor(x => x.Atmosphere.VisualTheme).NotNull().WithMessage("Gorsel tema gereklidir.");
            RuleFor(x => x.Atmosphere.SoundscapeMixer).NotNull().WithMessage("Ses mikseri gereklidir.");
            RuleFor(x => x.Atmosphere.RecommendedSession).NotNull().WithMessage("Onerilen seans bilgisi gereklidir.");

            When(x => x.Atmosphere.SoundscapeMixer != null, () =>
            {
                RuleFor(x => x.Atmosphere.SoundscapeMixer.Layer1Melody).NotNull().WithMessage("Melodi katmani gereklidir.");
                RuleFor(x => x.Atmosphere.SoundscapeMixer.Layer2Ambience).NotNull().WithMessage("Ambiyans katmani gereklidir.");
                RuleFor(x => x.Atmosphere.SoundscapeMixer.Layer3Noise).NotNull().WithMessage("Gurultu katmani gereklidir.");
                RuleFor(x => x.Atmosphere.SoundscapeMixer.Layer4Texture).NotNull().WithMessage("Doku katmani gereklidir.");
            });
        });

        RuleFor(x => x.PromptUsed)
            .MaximumLength(MaxPromptLength)
            .WithMessage($"Prompt en fazla {MaxPromptLength} karakter olabilir.");
    }
}
