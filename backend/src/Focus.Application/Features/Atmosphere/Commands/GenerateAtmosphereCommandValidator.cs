using FluentValidation;

namespace Focus.Application.Features.Atmosphere.Commands;

public class GenerateAtmosphereCommandValidator : AbstractValidator<GenerateAtmosphereCommand>
{
    public const int MaxPromptLength = 1000;
    public const int MaxOptionLength = 100;

    public GenerateAtmosphereCommandValidator()
    {
        RuleFor(x => x.Request).NotNull().WithMessage("Istek govdesi gereklidir.");

        When(x => x.Request != null, () =>
        {
            RuleFor(x => x.Request.Prompt)
                .MaximumLength(MaxPromptLength)
                .WithMessage($"Prompt en fazla {MaxPromptLength} karakter olabilir.");

            RuleFor(x => x.Request.FocusGoal).MaximumLength(MaxOptionLength);
            RuleFor(x => x.Request.MoodTarget).MaximumLength(MaxOptionLength);
            RuleFor(x => x.Request.PreferredAmbience).MaximumLength(MaxOptionLength);
            RuleFor(x => x.Request.VisualAesthetic).MaximumLength(MaxOptionLength);
            RuleFor(x => x.Request.TimeOfDay).MaximumLength(MaxOptionLength);
        });
    }
}
