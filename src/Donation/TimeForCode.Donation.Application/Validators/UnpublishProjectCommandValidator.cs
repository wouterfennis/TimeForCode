using FluentValidation;
using TimeForCode.Donation.Commands;

namespace TimeForCode.Donation.Application.Validators
{
    public class UnpublishProjectCommandValidator : AbstractValidator<UnpublishProjectCommand>
    {
        public UnpublishProjectCommandValidator()
        {
            RuleFor(x => x.ProjectId)
                .NotEmpty()
                .WithMessage("Project ID must not be empty.");

            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage("User ID must not be empty.");
        }
    }
}