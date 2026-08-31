using FluentValidation;
using TimeForCode.Donation.Commands;

namespace TimeForCode.Donation.Application.Validators
{
    public class CreateDonorOrganizationCommandValidator : AbstractValidator<CreateDonorOrganizationCommand>
    {
        public CreateDonorOrganizationCommandValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Name must not be empty.")
                .MaximumLength(200)
                .WithMessage("Name must not exceed 200 characters.");

            RuleFor(x => x.ContactEmail)
                .EmailAddress()
                .WithMessage("Contact email must be a valid email address.")
                .When(x => !string.IsNullOrEmpty(x.ContactEmail));

            RuleFor(x => x.Website)
                .Must(uri => Uri.TryCreate(uri, UriKind.Absolute, out _))
                .WithMessage("Website must be a valid URL.")
                .When(x => !string.IsNullOrEmpty(x.Website));
        }
    }
}