using FluentValidation;

namespace Focus.Application.Features.Coach.Commands;

public class SendCoachChatCommandValidator : AbstractValidator<SendCoachChatCommand>
{
    public const int MaxMessageLength = 1000;

    public SendCoachChatCommandValidator()
    {
        RuleFor(x => x.Message)
            .NotEmpty().WithMessage("Mesaj alani bos olamaz.")
            .MaximumLength(MaxMessageLength).WithMessage($"Mesaj en fazla {MaxMessageLength} karakter olabilir.");
    }
}
