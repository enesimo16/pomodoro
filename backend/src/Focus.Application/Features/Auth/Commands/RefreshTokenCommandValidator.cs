using FluentValidation;

namespace Focus.Application.Features.Auth.Commands;

public class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public const int MaxRefreshTokenLength = 512;

    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("Yenileme belirteci zorunludur.")
            .MaximumLength(MaxRefreshTokenLength).WithMessage("Yenileme belirteci geçersiz.");
    }
}
