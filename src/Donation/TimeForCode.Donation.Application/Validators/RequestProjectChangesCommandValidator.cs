using FluentValidation;
using TimeForCode.Donation.Commands;

namespace TimeForCode.Donation.Application.Validators
{
    public class RequestProjectChangesCommandValidator : AbstractValidator<RequestProjectChangesCommand>
    {
        public RequestProjectChangesCommandValidator()
        {
            RuleFor(x => x.ProjectId)
                .NotEmpty()
                .WithMessage("Project ID must not be empty.");

            RuleFor(x => x.Reason)
                .NotEmpty()
                .WithMessage("A reason is required when requesting changes.")
                .MaximumLength(1000)
                .WithMessage("The reason must not exceed 1000 characters.");
        }
    }
}