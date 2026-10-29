using FluentValidation;

namespace Focus.Application.Features.Auth.Commands;

public class GoogleLoginCommandValidator : AbstractValidator<GoogleLoginCommand>
{
    public const int MaxIdTokenLength = 4096;

    public GoogleLoginCommandValidator()
    {
        RuleFor(x => x.IdToken)
            .NotEmpty().WithMessage("Google kimlik belirteci zorunludur.")
            .MaximumLength(MaxIdTokenLength).WithMessage("Google kimlik belirteci geçersiz.");
    }
}
