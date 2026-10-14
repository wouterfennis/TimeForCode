using FluentValidation;
using TimeForCode.Donation.Commands;

namespace TimeForCode.Donation.Application.Validators
{
    public class AddContributionCommandValidator : AbstractValidator<AddContributionCommand>
    {
        public AddContributionCommandValidator()
        {
            RuleFor(x => x.Hours)
                .GreaterThan(0)
                .WithMessage("Hours must be greater than zero.");

            RuleFor(x => x.GithubReference)
                .Must(IsHttpsGithubLink)
                .When(x => x.GithubReference != null)
                .WithMessage("The GitHub reference must be an HTTPS link to github.com.");
        }

        private static bool IsHttpsGithubLink(Uri? reference)
        {
            return reference != null
                && reference.IsAbsoluteUri
                && string.Equals(reference.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
                && string.Equals(reference.Host, "github.com", StringComparison.OrdinalIgnoreCase);
        }
    }
}