using FluentAssertions;
using TimeForCode.Donation.Application.Validators;
using TimeForCode.Donation.Commands;

namespace TimeForCode.Donation.Application.Tests.Validators
{
    [TestClass]
    public class RegisterProjectCommandValidatorTests
    {
        private readonly RegisterProjectCommandValidator _sut = new();

        [TestMethod]
        public async Task Validate_ValidCommand_ReturnsSuccess()
        {
            var command = new RegisterProjectCommand
            {
                GithubRepositoryUrl = new Uri("https://github.com/owner/repo"),
                UserId = "user-123"
            };

            var result = await _sut.ValidateAsync(command);

            result.IsValid.Should().BeTrue();
        }

        [TestMethod]
        public async Task Validate_EmptyUserId_ReturnsFailure()
        {
            var command = new RegisterProjectCommand
            {
                GithubRepositoryUrl = new Uri("https://github.com/owner/repo"),
                UserId = string.Empty
            };

            var result = await _sut.ValidateAsync(command);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterProjectCommand.UserId));
        }

        [TestMethod]
        public async Task Validate_InvalidUriScheme_ReturnsFailure()
        {
            var command = new RegisterProjectCommand
            {
                GithubRepositoryUrl = new Uri("ftp://github.com/owner/repo"),
                UserId = "user-123"
            };

            var result = await _sut.ValidateAsync(command);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterProjectCommand.GithubRepositoryUrl));
        }
    }
}
