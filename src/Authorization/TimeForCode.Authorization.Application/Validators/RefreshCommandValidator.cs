using FluentValidation;
using TimeForCode.Authorization.Commands;

namespace TimeForCode.Authorization.Application.Validators
{
    public class RefreshCommandValidator : AbstractValidator<RefreshCommand>
    {
        public RefreshCommandValidator()
        {
            RuleFor(x => x.RefreshToken)
                .NotNull()
                .WithMessage("Refresh token must not be null.");

            RuleFor(x => x.RefreshToken.Token)
                .NotEmpty()
                .WithMessage("Refresh token value must not be empty.")
                .When(x => x.RefreshToken != null);
        }
    }
}