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
                .WithMessage("Hours must be greater than zero.")
                .LessThanOrEqualTo(AddContributionCommand.MaxHours)
                .WithMessage($"Hours must not exceed {AddContributionCommand.MaxHours}.");

            RuleFor(x => x.GithubReference)
                .Must(IsHttpsGithubLink)
                .When(x => x.GithubReference != null)
                .WithMessage("The GitHub reference must be an HTTPS link to a page on github.com.");
        }

        private static bool IsHttpsGithubLink(Uri? reference)
        {
            return reference != null
                && reference.IsAbsoluteUri
                && reference.OriginalString.Length <= AddContributionCommand.MaxReferenceLength
                && string.Equals(reference.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
                && string.Equals(reference.Host, "github.com", StringComparison.OrdinalIgnoreCase)
                && reference.IsDefaultPort
                && string.IsNullOrEmpty(reference.UserInfo)
                && string.IsNullOrEmpty(reference.Query)
                && reference.AbsolutePath.Length > 1;
        }
    }
}