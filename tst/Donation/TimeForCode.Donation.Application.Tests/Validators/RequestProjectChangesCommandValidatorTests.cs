using FluentAssertions;
using TimeForCode.Donation.Application.Validators;
using TimeForCode.Donation.Commands;

namespace TimeForCode.Donation.Application.Tests.Validators
{
    [TestClass]
    public class RequestProjectChangesCommandValidatorTests
    {
        private readonly RequestProjectChangesCommandValidator _sut = new();

        [TestMethod]
        public async Task Validate_ValidCommand_ReturnsSuccess()
        {
            var command = new RequestProjectChangesCommand { ProjectId = "project-123", Reason = "Please add a description" };

            var result = await _sut.ValidateAsync(command, CancellationToken.None);

            result.IsValid.Should().BeTrue();
        }

        [TestMethod]
        public async Task Validate_EmptyReason_ReturnsFailure()
        {
            var command = new RequestProjectChangesCommand { ProjectId = "project-123", Reason = " " };

            var result = await _sut.ValidateAsync(command, CancellationToken.None);

            result.Errors.Should().Contain(e => e.PropertyName == nameof(RequestProjectChangesCommand.Reason));
        }

        [TestMethod]
        public async Task Validate_TooLongReason_ReturnsFailure()
        {
            var command = new RequestProjectChangesCommand { ProjectId = "project-123", Reason = new string('x', 1001) };

            var result = await _sut.ValidateAsync(command, CancellationToken.None);

            result.Errors.Should().Contain(e => e.PropertyName == nameof(RequestProjectChangesCommand.Reason));
        }

        [TestMethod]
        public async Task Validate_EmptyProjectId_ReturnsFailure()
        {
            var command = new RequestProjectChangesCommand { ProjectId = string.Empty, Reason = "reason" };

            var result = await _sut.ValidateAsync(command, CancellationToken.None);

            result.Errors.Should().Contain(e => e.PropertyName == nameof(RequestProjectChangesCommand.ProjectId));
        }
    }
}