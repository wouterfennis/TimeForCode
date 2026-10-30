using FluentValidation;
using TimeForCode.Donation.Commands;
using TimeForCode.Donation.Domain;

namespace TimeForCode.Donation.Application.Validators
{
    public class CorrectContributionCommandValidator : AbstractValidator<CorrectContributionCommand>
    {
        public CorrectContributionCommandValidator()
        {
            RuleFor(x => x.Hours)
                .GreaterThan(0)
                .WithMessage("Hours must be greater than zero.")
                .LessThanOrEqualTo(AddContributionCommand.MaxHours)
                .WithMessage($"Hours must not exceed {AddContributionCommand.MaxHours}.");

            RuleFor(x => x.GithubReference)
                .Must(Contribution.IsValidGithubReference)
                .When(x => x.GithubReference != null)
                .WithMessage("The GitHub reference must be an HTTPS link to a page on github.com.");
        }
    }
}