using FluentValidation;

namespace Focus.Application.Features.Coach.Commands;

public class SubmitSessionCheckInCommandValidator : AbstractValidator<SubmitSessionCheckInCommand>
{
    public const int MaxIntentLength = 250;

    public SubmitSessionCheckInCommandValidator()
    {
        RuleFor(x => x.TargetIntent)
            .MaximumLength(MaxIntentLength)
            .WithMessage($"Hedef en fazla {MaxIntentLength} karakter olabilir.");
    }
}
