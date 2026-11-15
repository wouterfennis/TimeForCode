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

            // RFC 7636 S256: the base64url of a SHA-256 hash is exactly 43 characters.
            RuleFor(x => x.CodeChallenge)
                .NotEmpty()
                .Matches("^[A-Za-z0-9_-]{43}$")
                .WithMessage("Code challenge must be a 43 character base64url S256 challenge.");

            RuleFor(x => x.IdentityProvider)
                .IsInEnum()
                .WithMessage("Identity provider is not valid.");
        }
    }
}