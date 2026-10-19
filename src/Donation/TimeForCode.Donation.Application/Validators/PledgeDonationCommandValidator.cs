using FluentValidation;
using TimeForCode.Donation.Commands;

namespace TimeForCode.Donation.Application.Validators
{
    public class PledgeDonationCommandValidator : AbstractValidator<PledgeDonationCommand>
    {
        public PledgeDonationCommandValidator()
        {
            RuleFor(x => x.Hours)
                .GreaterThan(0)
                .WithMessage("Hours must be greater than zero.")
                .LessThanOrEqualTo(PledgeDonationCommand.MaxHours)
                .WithMessage($"Hours must not exceed {PledgeDonationCommand.MaxHours}.");

            RuleFor(x => x)
                .Must(x => x.AsIndividual ^ !string.IsNullOrWhiteSpace(x.DonorOrganizationId))
                .WithName("Donor")
                .WithMessage("A donation needs exactly one donor: an individual or a donor organization.");
        }
    }
}