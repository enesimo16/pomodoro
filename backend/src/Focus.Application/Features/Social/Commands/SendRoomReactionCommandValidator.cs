using FluentValidation;

namespace Focus.Application.Features.Social.Commands;

public class SendRoomReactionCommandValidator : AbstractValidator<SendRoomReactionCommand>
{
    public const int MaxReactionLength = 16;

    public SendRoomReactionCommandValidator()
    {
        RuleFor(x => x.RoomCode)
            .NotEmpty().WithMessage("Oda kodu zorunludur.")
            .MaximumLength(32).WithMessage("Oda kodu geçersiz.");

        RuleFor(x => x.Reaction)
            .NotEmpty().WithMessage("Reaksiyon zorunludur.")
            .Must(r => r.Trim().Length > 0).WithMessage("Reaksiyon zorunludur.")
            .MaximumLength(MaxReactionLength).WithMessage("Reaksiyon en fazla 16 karakter olabilir.")
            .Must(r => !r.Any(char.IsControl)).WithMessage("Reaksiyon geçersiz karakter içeriyor.");
    }
}
