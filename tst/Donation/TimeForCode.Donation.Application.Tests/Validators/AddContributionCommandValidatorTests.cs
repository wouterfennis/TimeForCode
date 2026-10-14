using FluentAssertions;
using TimeForCode.Donation.Application.Validators;
using TimeForCode.Donation.Commands;

namespace TimeForCode.Donation.Application.Tests.Validators
{
    [TestClass]
    public class AddContributionCommandValidatorTests
    {
        private readonly AddContributionCommandValidator _sut = new();

        private static AddContributionCommand Command(int hours = 3, Uri? githubReference = null) => new()
        {
            ProjectId = "5f43a0e74b12c84f1b000001",
            UserId = "user-1",
            GithubLogin = "octocat",
            AvatarUrl = "https://avatars.githubusercontent.com/u/1",
            Hours = hours,
            GithubReference = githubReference
        };

        [TestMethod]
        public async Task Validate_ValidCommandWithoutReference_ReturnsSuccess()
        {
            var result = await _sut.ValidateAsync(Command(), CancellationToken.None);

            result.IsValid.Should().BeTrue();
        }

        [TestMethod]
        public async Task Validate_GithubPullRequestReference_ReturnsSuccess()
        {
            var result = await _sut.ValidateAsync(Command(githubReference: new Uri("https://github.com/owner/repo/pull/12")), CancellationToken.None);

            result.IsValid.Should().BeTrue();
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(-2)]
        public async Task Validate_HoursNotPositive_ReturnsFailure(int hours)
        {
            var result = await _sut.ValidateAsync(Command(hours), CancellationToken.None);

            result.Errors.Should().Contain(e => e.PropertyName == nameof(AddContributionCommand.Hours));
        }

        [TestMethod]
        [DataRow("http://github.com/owner/repo/pull/12")]
        [DataRow("https://example.com/owner/repo/pull/12")]
        [DataRow("https://github.com.evil.example/owner/repo/pull/12")]
        public async Task Validate_ReferenceNotAnHttpsGithubLink_ReturnsFailure(string reference)
        {
            var result = await _sut.ValidateAsync(Command(githubReference: new Uri(reference)), CancellationToken.None);

            result.Errors.Should().Contain(e => e.PropertyName == nameof(AddContributionCommand.GithubReference));
        }

        [TestMethod]
        public async Task Validate_HoursAboveMaximum_ReturnsFailure()
        {
            var result = await _sut.ValidateAsync(Command(AddContributionCommand.MaxHours + 1), CancellationToken.None);

            result.Errors.Should().Contain(e => e.PropertyName == nameof(AddContributionCommand.Hours));
        }

        [TestMethod]
        [DataRow("https://github.com/")]
        [DataRow("https://github.com:8443/owner/repo/pull/12")]
        [DataRow("https://user:secret@github.com/owner/repo/pull/12")]
        [DataRow("https://github.com/owner/repo/pull/12?redir=https://evil.example")]
        public async Task Validate_ReferenceNotAPathOnGithub_ReturnsFailure(string reference)
        {
            var result = await _sut.ValidateAsync(Command(githubReference: new Uri(reference)), CancellationToken.None);

            result.Errors.Should().Contain(e => e.PropertyName == nameof(AddContributionCommand.GithubReference));
        }

        [TestMethod]
        public async Task Validate_ReferenceTooLong_ReturnsFailure()
        {
            var reference = new Uri("https://github.com/owner/repo/pull/" + new string('1', AddContributionCommand.MaxReferenceLength));

            var result = await _sut.ValidateAsync(Command(githubReference: reference), CancellationToken.None);

            result.Errors.Should().Contain(e => e.PropertyName == nameof(AddContributionCommand.GithubReference));
        }
    }
}