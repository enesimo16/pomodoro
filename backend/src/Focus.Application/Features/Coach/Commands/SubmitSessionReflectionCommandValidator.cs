using FluentValidation;

namespace Focus.Application.Features.Coach.Commands;

public class SubmitSessionReflectionCommandValidator : AbstractValidator<SubmitSessionReflectionCommand>
{
    public const int MaxNoteLength = 350;

    public SubmitSessionReflectionCommandValidator()
    {
        RuleFor(x => x.DistractionNote)
            .MaximumLength(MaxNoteLength)
            .WithMessage($"Not en fazla {MaxNoteLength} karakter olabilir.");
    }
}
