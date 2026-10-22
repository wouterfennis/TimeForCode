using FluentAssertions;
using Reqnroll;
using TimeForCode.Donation.Api.Client;
using TimeForCode.Donation.Specifications.Mocking;

namespace TimeForCode.Donation.Specifications.Steps
{
    /// <summary>Steps for <c>@live</c> scenarios: they drive the real stack through the API only, and never touch the in-process mocks.</summary>
    internal partial class ProjectSteps
    {
        private string? _liveRepositoryUrl;
        private readonly List<string> _liveProjectIds = [];

        [Given("The user has published a new repository on the live platform")]
        [When("The user publishes a new repository on the live platform")]
        public Task GivenTheUserHasPublishedANewRepositoryOnTheLivePlatformAsync()
        {
            return PublishLiveRepositoryAsync($"https://github.com/live-owner/live{Guid.NewGuid():N}");
        }

        [When("The user publishes the same repository on the live platform again")]
        public Task WhenTheUserPublishesTheSameRepositoryOnTheLivePlatformAgainAsync()
        {
            return PublishLiveRepositoryAsync(_liveRepositoryUrl!);
        }

        [Given("The user has an active project registered on the live platform")]
        public async Task GivenTheUserHasAnActiveProjectRegisteredOnTheLivePlatformAsync()
        {
            await GivenTheUserHasPublishedANewRepositoryOnTheLivePlatformAsync();
            _exception.Should().BeNull();

            await _donationClient.SubmitProjectForReviewAsync(_registeredProjectId!);

            using HttpClient administratorHttpClient = LiveHost.CreateClient(
                LiveHost.RequiredBaseAddress,
                new BearerTokenHandler { Token = await LiveAuthentication.GetAdminTokenAsync() });
            await new DonationClient(administratorHttpClient).ApproveProjectAsync(_registeredProjectId!);
        }

        [When("The user searches the projects for the name of the project")]
        public async Task WhenTheUserSearchesTheProjectsForTheNameOfTheProjectAsync()
        {
            var maintained = await _donationClient.GetMaintainedProjectAsync(_registeredProjectId!);

            await CaptureAsync(async () => _getProjectsResult = await _donationClient.GetProjectsAsync(null, null, maintained.Project.Name, null, null));
        }

        [Then("The project is in draft on the live platform")]
        public async Task ThenTheProjectIsInDraftOnTheLivePlatformAsync()
        {
            var maintained = await _donationClient.GetMaintainedProjectAsync(_registeredProjectId!);

            maintained.Project.Status.ToString().Should().Be("Draft");
        }

        [Then("Only the project of the user is included in the results on the live platform")]
        public void ThenOnlyTheProjectOfTheUserIsIncludedInTheResultsOnTheLivePlatform()
        {
            _exception.Should().BeNull();
            _getProjectsResult!.Projects.Select(p => p.Id).Should().Equal(_registeredProjectId);
        }

        [Then("The contributions of the project add up to {int} hours on the live platform")]
        public void ThenTheContributionsOfTheProjectAddUpToHoursOnTheLivePlatform(int hours)
        {
            _exception.Should().BeNull();
            _getContributionsResult!.Contributions.Sum(c => c.Hours).Should().Be(hours);
        }

        [Then("The donations of the project add up to {int} hours committed on the live platform")]
        public void ThenTheDonationsOfTheProjectAddUpToHoursCommittedOnTheLivePlatform(int hours)
        {
            _exception.Should().BeNull();
            _getDonationsResult!.Donations.Sum(d => d.HoursCommitted).Should().Be(hours);
        }

        [AfterScenario(LiveHost.Tag)]
        public async Task ArchiveLiveProjectsAsync()
        {
            foreach (string projectId in _liveProjectIds)
            {
                try
                {
                    _bearerTokenHandler.Token = await LiveAuthentication.GetUserTokenAsync();
                    await _donationClient.ArchiveProjectAsync(projectId);
                }
                catch (Exception exception) when (exception is ApiException or HttpRequestException or InvalidOperationException)
                {
                    // Best-effort clean-up: a project in draft cannot be archived, and a failure here must not hide the scenario result.
                    Console.WriteLine($"Could not archive live project {projectId}: {exception.Message}");
                }
            }
        }

        private async Task PublishLiveRepositoryAsync(string repositoryUrl)
        {
            _liveRepositoryUrl = repositoryUrl;
            _exception = null;

            await CaptureAsync(async () =>
            {
                _registerResult = await _donationClient.RegisterProjectAsync(new RegisterProjectRequest
                {
                    GithubRepositoryUrl = new Uri(repositoryUrl)
                });
                _registeredProjectId = _registerResult.ProjectId;
                _liveProjectIds.Add(_registeredProjectId);
            });
        }
    }
}