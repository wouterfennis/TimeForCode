using FluentAssertions;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using TimeForCode.Donation.Application.Behaviours;

namespace TimeForCode.Donation.Application.Tests.Behaviours
{
    [TestClass]
    public class ValidationBehaviourTests
    {
        public sealed record TestRequest(string Value) : IRequest<string>;

        private sealed class AlwaysValidValidator : AbstractValidator<TestRequest>
        {
            public AlwaysValidValidator()
            {
                RuleFor(x => x.Value).NotEmpty();
            }
        }

        private sealed class AlwaysInvalidValidator : AbstractValidator<TestRequest>
        {
            public AlwaysInvalidValidator()
            {
                RuleFor(x => x.Value).Must(_ => false).WithMessage("Field is required");
            }
        }

        [TestMethod]
        public async Task Handle_NoValidators_CallsNext()
        {
            var sut = new ValidationBehaviour<TestRequest, string>(
                [],
                NullLogger<ValidationBehaviour<TestRequest, string>>.Instance);

            var nextCalled = false;
            var result = await sut.Handle(new TestRequest("value"), _ =>
            {
                nextCalled = true;
                return Task.FromResult("ok");
            }, CancellationToken.None);

            nextCalled.Should().BeTrue();
            result.Should().Be("ok");
        }

        [TestMethod]
        public async Task Handle_ValidRequest_CallsNext()
        {
            var sut = new ValidationBehaviour<TestRequest, string>(
                [new AlwaysValidValidator()],
                NullLogger<ValidationBehaviour<TestRequest, string>>.Instance);

            var nextCalled = false;
            var result = await sut.Handle(new TestRequest("value"), _ =>
            {
                nextCalled = true;
                return Task.FromResult("ok");
            }, CancellationToken.None);

            nextCalled.Should().BeTrue();
            result.Should().Be("ok");
        }

        [TestMethod]
        public async Task Handle_InvalidRequest_ThrowsValidationException()
        {
            var sut = new ValidationBehaviour<TestRequest, string>(
                [new AlwaysInvalidValidator()],
                NullLogger<ValidationBehaviour<TestRequest, string>>.Instance);

            var act = () => sut.Handle(new TestRequest("value"), _ => Task.FromResult("ok"), CancellationToken.None);

            await act.Should().ThrowAsync<ValidationException>()
                .WithMessage("*Field is required*");
        }
    }
}
