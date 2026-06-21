using FluentValidation;
using TimeForCode.Donation.Commands;

namespace TimeForCode.Donation.Application.Validators
{
    public class RegisterProjectCommandValidator : AbstractValidator<RegisterProjectCommand>
    {
        public RegisterProjectCommandValidator()
        {
            RuleFor(x => x.GithubRepositoryUrl)
                .NotNull()
                .WithMessage("GitHub repository URL must not be null.")
                .Must(uri => uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp)
                .WithMessage("GitHub repository URL must use HTTP or HTTPS scheme.")
                .When(x => x.GithubRepositoryUrl != null);

            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage("User ID must not be empty.");
        }
    }
}