using FluentAssertions;
using TimeForCode.Authorization.Application.Validators;
using TimeForCode.Authorization.Commands;

namespace TimeForCode.Authorization.Application.Tests.Validators
{
    [TestClass]
    public class CallbackCommandValidatorTests
    {
        private readonly CallbackCommandValidator _sut = new();

        [TestMethod]
        public async Task Validate_ValidCommand_ReturnsSuccess()
        {
            var command = new CallbackCommand
            {
                Code = "valid-code",
                State = "valid-state"
            };

            var result = await _sut.ValidateAsync(command);

            result.IsValid.Should().BeTrue();
        }

        [TestMethod]
        public async Task Validate_EmptyCode_ReturnsFailure()
        {
            var command = new CallbackCommand
            {
                Code = string.Empty,
                State = "valid-state"
            };

            var result = await _sut.ValidateAsync(command);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(CallbackCommand.Code));
        }

        [TestMethod]
        public async Task Validate_EmptyState_ReturnsFailure()
        {
            var command = new CallbackCommand
            {
                Code = "valid-code",
                State = string.Empty
            };

            var result = await _sut.ValidateAsync(command);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(CallbackCommand.State));
        }
    }
}