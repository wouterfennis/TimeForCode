using FluentValidation;
using TimeForCode.Authorization.Commands;

namespace TimeForCode.Authorization.Application.Validators
{
    public class CallbackCommandValidator : AbstractValidator<CallbackCommand>
    {
        public CallbackCommandValidator()
        {
            RuleFor(x => x.Code)
                .NotEmpty()
                .WithMessage("Code must not be empty.");

            RuleFor(x => x.State)
                .NotEmpty()
                .WithMessage("State must not be empty.");
        }
    }
}