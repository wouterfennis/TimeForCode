using FluentValidation;
using MongoDB.Bson;
using TimeForCode.Donation.Commands;
using TimeForCode.Donation.Values;

namespace TimeForCode.Donation.Application.Validators
{
    public class UpdateContributorProfileCommandValidator : AbstractValidator<UpdateContributorProfileCommand>
    {
        public UpdateContributorProfileCommandValidator()
        {
            RuleFor(x => x.Role)
                .MaximumLength(ContributorProfileLimits.MaxRoleLength)
                .WithMessage($"Role must not exceed {ContributorProfileLimits.MaxRoleLength} characters.");

            RuleFor(x => x.Skills)
                .Must(skills => skills.Count <= ContributorProfileLimits.MaxSkills)
                .WithMessage($"A profile can hold at most {ContributorProfileLimits.MaxSkills} skills.");

            RuleForEach(x => x.Skills)
                .Must(skill => skill.Trim().Length <= ContributorProfileLimits.MaxSkillLength)
                .WithMessage($"A skill must not exceed {ContributorProfileLimits.MaxSkillLength} characters.");

            RuleFor(x => x.HoursAvailable)
                .InclusiveBetween(0, ContributorProfileLimits.MaxHoursAvailable)
                .When(x => x.HoursAvailable.HasValue)
                .WithMessage($"Hours available must be between 0 and {ContributorProfileLimits.MaxHoursAvailable}.");

            RuleFor(x => x.DonorOrganizationId)
                .Must(id => ObjectId.TryParse(id, out _))
                .When(x => x.DonorOrganizationId != null)
                .WithMessage("The donor organization identifier is not valid.");
        }
    }
}