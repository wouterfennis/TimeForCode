using AwesomeAssertions;
using TimeForCode.Donation.Application.Validators;
using TimeForCode.Donation.Commands;

namespace TimeForCode.Donation.Application.Tests.Validators
{
    [TestClass]
    public class CorrectContributionCommandValidatorTests
    {
        private readonly CorrectContributionCommandValidator _sut = new();

        private static CorrectContributionCommand Command(int hours = 5, Uri? githubReference = null) => new()
        {
            ProjectId = "5f43a0e74b12c84f1b000001",
            ContributionId = "5f43a0e74b12c84f1b0000f1",
            UserId = "user-1",
            IsAdministrator = false,
            Hours = hours,
            GithubReference = githubReference
        };

        [TestMethod]
        public async Task Validate_ValidCommandWithGithubReference_ReturnsSuccess()
        {
            var result = await _sut.ValidateAsync(Command(githubReference: new Uri("https://github.com/owner/repo/pull/34")), CancellationToken.None);

            result.IsValid.Should().BeTrue();
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(-2)]
        [DataRow(AddContributionCommand.MaxHours + 1)]
        public async Task Validate_HoursOutOfRange_ReturnsFailure(int hours)
        {
            var result = await _sut.ValidateAsync(Command(hours), CancellationToken.None);

            result.IsValid.Should().BeFalse();
        }

        [TestMethod]
        [DataRow("http://github.com/owner/repo/pull/12")]
        [DataRow("https://example.com/owner/repo/pull/12")]
        public async Task Validate_ReferenceNotAnHttpsGithubLink_ReturnsFailure(string reference)
        {
            var result = await _sut.ValidateAsync(Command(githubReference: new Uri(reference)), CancellationToken.None);

            result.IsValid.Should().BeFalse();
        }
    }
}