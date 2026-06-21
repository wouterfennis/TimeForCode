using FluentAssertions;
using TimeForCode.Donation.Application.Validators;
using TimeForCode.Donation.Commands;

namespace TimeForCode.Donation.Application.Tests.Validators
{
    [TestClass]
    public class UnpublishProjectCommandValidatorTests
    {
        private readonly UnpublishProjectCommandValidator _sut = new();

        [TestMethod]
        public async Task Validate_ValidCommand_ReturnsSuccess()
        {
            var command = new UnpublishProjectCommand
            {
                ProjectId = "project-123",
                UserId = "user-123"
            };

            var result = await _sut.ValidateAsync(command);

            result.IsValid.Should().BeTrue();
        }

        [TestMethod]
        public async Task Validate_EmptyProjectId_ReturnsFailure()
        {
            var command = new UnpublishProjectCommand
            {
                ProjectId = string.Empty,
                UserId = "user-123"
            };

            var result = await _sut.ValidateAsync(command);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(UnpublishProjectCommand.ProjectId));
        }

        [TestMethod]
        public async Task Validate_EmptyUserId_ReturnsFailure()
        {
            var command = new UnpublishProjectCommand
            {
                ProjectId = "project-123",
                UserId = string.Empty
            };

            var result = await _sut.ValidateAsync(command);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(UnpublishProjectCommand.UserId));
        }
    }
}
