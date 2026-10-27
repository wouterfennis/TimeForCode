using AwesomeAssertions;
using MongoDB.Bson;
using Reqnroll;
using TimeForCode.Donation.Api.Client;
using TimeForCode.Donation.Domain;
using TimeForCode.Donation.Specifications.Mocking;

namespace TimeForCode.Donation.Specifications.Steps
{
    /// <summary>Anonymising the contributions of a user whose account was deleted.</summary>
    [Binding]
    internal class ContributorAnonymisationSteps
    {
        private const string OtherUserId = DonationFixtures.OtherUserId;
        private const string OtherUserLogin = DonationFixtures.OtherUserLogin;
        private const string OtherUserAvatarUrl = DonationFixtures.OtherUserAvatarUrl;
        private const string UserWithoutContributionsId = "507f1f77bcf86cd799439099";

        private readonly IDonationClient _donationClient;
        private readonly BearerTokenHandler _bearerTokenHandler;
        private readonly DonationContext _context;
        private readonly DonationFixtures _fixtures;

        private ObjectId? _anonymisedContributorId;

        public ContributorAnonymisationSteps(IDonationClient donationClient, BearerTokenHandler bearerTokenHandler, DonationContext context, DonationFixtures fixtures)
        {
            _donationClient = donationClient;
            _bearerTokenHandler = bearerTokenHandler;
            _context = context;
            _fixtures = fixtures;
        }

        [Given("The account of the user has been deleted at the time for code platform")]
        [When("The account of the user is deleted at the time for code platform")]
        public Task WhenTheAccountOfTheUserIsDeletedAtTheTimeForCodePlatformAsync()
        {
            _anonymisedContributorId = _fixtures.Contributors.Contributors
                .SingleOrDefault(c => c.PlatformUserId == Constants.TestUserId)?.Id;

            return AnonymiseAsServiceAsync(Constants.TestUserId);
        }

        [When("The account of a user without contributions is deleted at the time for code platform")]
        public Task WhenTheAccountOfAUserWithoutContributionsIsDeletedAtTheTimeForCodePlatformAsync()
        {
            return AnonymiseAsServiceAsync(UserWithoutContributionsId);
        }

        [When("The user asks to anonymise a contributor")]
        public Task WhenTheUserAsksToAnonymiseAContributorAsync()
        {
            return _context.CaptureAsync(() => _donationClient.AnonymiseContributorAsync(Constants.TestUserId));
        }

        [Then("The contributor of the user is shown as a deleted user")]
        public void ThenTheContributorOfTheUserIsShownAsADeletedUser()
        {
            _context.LastException.Should().BeNull();
            _anonymisedContributorId.Should().NotBeNull();

            var contributor = _fixtures.Contributors.Contributors.Single(c => c.Id == _anonymisedContributorId);
            contributor.GithubLogin.Should().Be(Contributor.DeletedUserLogin);
            contributor.AvatarUrl.Should().Be(Contributor.DeletedUserAvatarUrl);
            contributor.PlatformUserId.Should().NotBe(Constants.TestUserId);
        }

        [Then("The contributions of the project no longer show the login or avatar of the user")]
        public void ThenTheContributionsOfTheProjectNoLongerShowTheLoginOrAvatarOfTheUser()
        {
            _context.ContributionsResult.Should().NotBeNull();
            _context.ContributionsResult!.Contributions.Should().NotBeEmpty();
            _context.ContributionsResult.Contributions.Should().OnlyContain(c =>
                c.ContributorGitHubLogin == Contributor.DeletedUserLogin
                && c.ContributorAvatarUrl == Contributor.DeletedUserAvatarUrl);
        }

        [Then("The contributor of the other user is unchanged")]
        public void ThenTheContributorOfTheOtherUserIsUnchanged()
        {
            var other = _fixtures.Contributors.Contributors.Single(c => c.PlatformUserId == OtherUserId);
            other.GithubLogin.Should().Be(OtherUserLogin);
            other.AvatarUrl.Should().Be(OtherUserAvatarUrl);

            _context.ContributionsResult.Should().NotBeNull();
            _context.ContributionsResult!.Contributions.Should().Contain(c =>
                c.ContributorGitHubLogin == OtherUserLogin && c.ContributorAvatarUrl == OtherUserAvatarUrl);
        }

        [Then("The anonymisation is confirmed")]
        public void ThenTheAnonymisationIsConfirmed()
        {
            _context.LastException.Should().BeNull();
        }

        private async Task AnonymiseAsServiceAsync(string userId)
        {
            _bearerTokenHandler.Token = AuthenticationSteps.GenerateTestToken("authorization-service", "service");

            await _context.CaptureAsync(() => _donationClient.AnonymiseContributorAsync(userId));

            if (_context.LastException == null && _context.RegisteredProjectId != null)
            {
                string projectId = _context.RegisteredProjectId;
                await _context.CaptureAsync(async () => _context.ContributionsResult = await _donationClient.GetContributionsAsync(projectId));
            }
        }
    }
}