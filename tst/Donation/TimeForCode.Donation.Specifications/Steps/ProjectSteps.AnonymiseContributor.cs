using FluentAssertions;
using Microsoft.AspNetCore.Http;
using MongoDB.Bson;
using Reqnroll;
using TimeForCode.Donation.Domain;

namespace TimeForCode.Donation.Specifications.Steps
{
    internal partial class ProjectSteps
    {
        private const string OtherUserId = "507f1f77bcf86cd799439021";
        private const string OtherUserLogin = "hubot";
        private const string OtherUserAvatarUrl = "https://avatars.githubusercontent.com/u/480938?v=4";
        private const string UserWithoutContributionsId = "507f1f77bcf86cd799439099";

        private ObjectId? _anonymisedContributorId;

        [Given("Another user has already logged {int} hours on the project")]
        public void GivenAnotherUserHasAlreadyLoggedHoursOnTheProject(int hours)
        {
            var other = Contributor.Create(OtherUserId, OtherUserLogin, OtherUserAvatarUrl);
            Contributors.Add(other);
            var nextMoment = FirstLoggedAt.AddDays(Contributions.Contributions.Count);
            Contributions.CreateAsync(NewContribution(ObjectIdOf(_registeredProjectId!), other, hours, nextMoment));
        }

        [Given("The user has already logged {int} hours on a published project")]
        public void GivenTheUserHasAlreadyLoggedHoursOnAPublishedProject(int hours)
        {
            GivenThereIsAPublishedProjectOnTheTimeForCodePlatform();
            GivenTheUserHasAlreadyLoggedHoursOnTheProject(hours);
        }

        [Given("The account of the user has been deleted at the time for code platform")]
        [When("The account of the user is deleted at the time for code platform")]
        public Task WhenTheAccountOfTheUserIsDeletedAtTheTimeForCodePlatformAsync()
        {
            _anonymisedContributorId = Contributors.Contributors
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
            return CaptureAsync(() => _donationClient.AnonymiseContributorAsync(Constants.TestUserId));
        }

        [Then("The contributor of the user is shown as a deleted user")]
        public void ThenTheContributorOfTheUserIsShownAsADeletedUser()
        {
            _exception.Should().BeNull();
            _anonymisedContributorId.Should().NotBeNull();

            var contributor = Contributors.Contributors.Single(c => c.Id == _anonymisedContributorId);
            contributor.GithubLogin.Should().Be(Contributor.DeletedUserLogin);
            contributor.AvatarUrl.Should().Be(Contributor.DeletedUserAvatarUrl);
            contributor.PlatformUserId.Should().NotBe(Constants.TestUserId);
        }

        [Then("The contributions of the project no longer show the login or avatar of the user")]
        public void ThenTheContributionsOfTheProjectNoLongerShowTheLoginOrAvatarOfTheUser()
        {
            _getContributionsResult.Should().NotBeNull();
            _getContributionsResult!.Contributions.Should().NotBeEmpty();
            _getContributionsResult.Contributions.Should().OnlyContain(c =>
                c.ContributorGitHubLogin == Contributor.DeletedUserLogin
                && c.ContributorAvatarUrl == Contributor.DeletedUserAvatarUrl);
        }

        [Then("The contributor of the other user is unchanged")]
        public void ThenTheContributorOfTheOtherUserIsUnchanged()
        {
            var other = Contributors.Contributors.Single(c => c.PlatformUserId == OtherUserId);
            other.GithubLogin.Should().Be(OtherUserLogin);
            other.AvatarUrl.Should().Be(OtherUserAvatarUrl);

            _getContributionsResult.Should().NotBeNull();
            _getContributionsResult!.Contributions.Should().Contain(c =>
                c.ContributorGitHubLogin == OtherUserLogin && c.ContributorAvatarUrl == OtherUserAvatarUrl);
        }

        [Then("The anonymisation is confirmed")]
        public void ThenTheAnonymisationIsConfirmed()
        {
            _exception.Should().BeNull();
        }

        [Then("The user is informed they are not allowed to anonymise a contributor")]
        public void ThenTheUserIsInformedTheyAreNotAllowedToAnonymiseAContributor()
        {
            _exception.Should().NotBeNull();
            _exception!.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        }

        private async Task AnonymiseAsServiceAsync(string userId)
        {
            _bearerTokenHandler.Token = AuthenticationSteps.GenerateTestToken("authorization-service", "service");

            await CaptureAsync(() => _donationClient.AnonymiseContributorAsync(userId));

            if (_exception == null && _registeredProjectId != null)
            {
                await GetContributionsAsync(_registeredProjectId);
            }
        }
    }
}