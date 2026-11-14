using FluentValidation;
using TimeForCode.Authorization.Commands;

namespace TimeForCode.Authorization.Application.Validators
{
    public class ExchangeCodeCommandValidator : AbstractValidator<ExchangeCodeCommand>
    {
        public ExchangeCodeCommandValidator()
        {
            RuleFor(x => x.Code)
                .NotEmpty()
                .WithMessage("Code must not be empty.");

            // RFC 7636: the verifier is 43 to 128 unreserved characters.
            RuleFor(x => x.CodeVerifier)
                .NotEmpty()
                .Matches("^[A-Za-z0-9._~-]{43,128}$")
                .WithMessage("Code verifier must be 43 to 128 unreserved characters.");
        }
    }
}