using FluentValidation;
using TimeForCode.Authorization.Commands;

namespace TimeForCode.Authorization.Application.Validators
{
    public class LoginCommandValidator : AbstractValidator<LoginCommand>
    {
        public LoginCommandValidator()
        {
            RuleFor(x => x.RedirectUri)
                .NotNull()
                .WithMessage("Redirect URI must not be null.");

            RuleFor(x => x.IdentityProvider)
                .IsInEnum()
                .WithMessage("Identity provider is not valid.");
        }
    }
}