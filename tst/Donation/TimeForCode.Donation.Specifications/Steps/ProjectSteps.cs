using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using Moq;
using Reqnroll;
using TimeForCode.Donation.Api.Client;
using TimeForCode.Donation.Application.Interfaces;
using TimeForCode.Donation.Commands;
using TimeForCode.Donation.Domain;
using TimeForCode.Donation.Specifications.TestBuilder;
using TimeForCode.Donation.Values;

namespace TimeForCode.Donation.Specifications.Steps
{
    /// <summary>Registering a repository as a project and reading one project.</summary>
    [Binding]
    internal class ProjectSteps
    {
        private readonly IDonationClient _donationClient;
        private readonly IServiceProvider _provider;
        private readonly DonationContext _context;
        private readonly DonationFixtures _fixtures;

        private ProjectResponse? _getProjectByIdResult;

        public ProjectSteps(IDonationClient donationClient, IServiceProvider provider, DonationContext context, DonationFixtures fixtures)
        {
            _donationClient = donationClient;
            _provider = provider;
            _context = context;
            _fixtures = fixtures;
        }

        [Given("There is a project published by another user")]
        public void GivenThereIsAProjectPublishedByAnotherUser()
        {
            var anotherUsersProject = new Project
            {
                Id = new ObjectId(Constants.TestProjectId),
                Snapshot = ProjectBuilder.BuildSnapshot(),
                GithubRepositoryUrl = new Uri(Constants.TestGithubRepositoryUrl),
                PublishedByUserId = "another-user-id",
                PublishedAt = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero)
            }.InStatus(TimeForCode.Donation.Values.ProjectStatus.Active);

            _fixtures.ProjectRepository.Setup(x => x.GetByIdAsync(anotherUsersProject.Id.ToString()))
                .ReturnsAsync(anotherUsersProject);

            _fixtures.TrackProject(anotherUsersProject);
            _context.RegisteredProjectId = anotherUsersProject.Id.ToString();
        }

        [Given("The repository is public and active on the external platform")]
        public void GivenTheRepositoryIsPublicAndActiveOnTheExternalPlatform()
        {
            var mockGithubService = _provider.GetRequiredService<Mock<IGithubRepositoryApiService>>();
            mockGithubService.Setup(x => x.GetRepositoryMetadataAsync(It.IsAny<Uri>()))
                .ReturnsAsync(Result<GithubSnapshot>.Success(ProjectBuilder.BuildSnapshot()));
        }

        [Given("The repository is private on the external platform")]
        public void GivenTheRepositoryIsPrivateOnTheExternalPlatform()
        {
            var mockGithubService = _provider.GetRequiredService<Mock<IGithubRepositoryApiService>>();
            var privateSnapshot = ProjectBuilder.BuildSnapshot() with { IsPrivate = true };
            mockGithubService.Setup(x => x.GetRepositoryMetadataAsync(It.IsAny<Uri>()))
                .ReturnsAsync(Result<GithubSnapshot>.Success(privateSnapshot));
        }

        [Given("The repository is archived on the external platform")]
        public void GivenTheRepositoryIsArchivedOnTheExternalPlatform()
        {
            var mockGithubService = _provider.GetRequiredService<Mock<IGithubRepositoryApiService>>();
            var archivedSnapshot = ProjectBuilder.BuildSnapshot() with { IsArchived = true };
            mockGithubService.Setup(x => x.GetRepositoryMetadataAsync(It.IsAny<Uri>()))
                .ReturnsAsync(Result<GithubSnapshot>.Success(archivedSnapshot));
        }

        [Given("The external platform does not have the repository")]
        public void GivenTheExternalPlatformDoesNotHaveTheRepository()
        {
            var mockGithubService = _provider.GetRequiredService<Mock<IGithubRepositoryApiService>>();
            mockGithubService.Setup(x => x.GetRepositoryMetadataAsync(It.IsAny<Uri>()))
                .ReturnsAsync(Result<GithubSnapshot>.Failure("Failed to retrieve repository information from GitHub."));
        }

        [Given("The user has already published the repository on the time for code platform")]
        public void GivenTheUserHasAlreadyPublishedTheRepositoryOnTheTimeForCodePlatform()
        {
            var existingProject = ProjectBuilder.BuildActive();

            _fixtures.ProjectRepository.Setup(x => x.GetByGithubUrlAsync(It.IsAny<Uri>()))
                .ReturnsAsync(existingProject);
            _fixtures.ProjectRepository.Setup(x => x.GetByIdAsync(existingProject.Id.ToString()))
                .ReturnsAsync(existingProject);

            var mockGithubService = _provider.GetRequiredService<Mock<IGithubRepositoryApiService>>();
            mockGithubService.Setup(x => x.GetRepositoryMetadataAsync(It.IsAny<Uri>()))
                .ReturnsAsync(Result<GithubSnapshot>.Success(ProjectBuilder.BuildSnapshot()));

            _context.RegisteredProjectId = existingProject.Id.ToString();
        }

        [Given("There is a published project on the time for code platform")]
        public void GivenThereIsAPublishedProjectOnTheTimeForCodePlatform()
        {
            _fixtures.PublishActiveProject();
        }

        [When("The user publishes the repository on the time for code platform")]
        public async Task WhenTheUserPublishesTheRepositoryOnTheTimeForCodePlatformAsync()
        {
            _fixtures.ProjectRepository.Setup(x => x.CreateAsync(It.IsAny<Project>()))
                .Callback<Project>(p => _context.RegisteredProjectId = p.Id.ToString())
                .Returns(Task.CompletedTask);

            await _context.CaptureAsync(() => _donationClient.RegisterProjectAsync(new RegisterProjectRequest
            {
                GithubRepositoryUrl = new Uri(Constants.TestGithubRepositoryUrl)
            }));
        }

        [When("The user requests the project details")]
        [When("The user requests the project details without an account")]
        public Task WhenTheUserRequestsTheProjectDetailsAsync()
        {
            string projectId = _context.RegisteredProjectId ?? Constants.TestProjectId;

            return _context.CaptureAsync(async () => _getProjectByIdResult = await _donationClient.GetProjectByIdAsync(projectId));
        }

        [When("^The user requests the project with the identifier \"(.*)\"$")]
        public Task WhenTheUserRequestsTheProjectWithTheIdentifierAsync(string identifier)
        {
            return _context.CaptureAsync(async () => _getProjectByIdResult = await _donationClient.GetProjectByIdAsync(identifier));
        }

        [Then("The project is registered on the time for code platform")]
        public void ThenTheProjectIsRegisteredOnTheTimeForCodePlatform()
        {
            // The in-process and the live When steps both record the new project in the shared context.
            _context.LastException.Should().BeNull();
            _context.RegisteredProjectId.Should().NotBeNullOrEmpty();
        }

        [Then("The stored project metadata includes the following fields:")]
        public void ThenTheStoredProjectMetadataIncludesTheFollowingFields(DataTable table)
        {
            _context.LastException.Should().BeNull();
            _context.RegisteredProjectId.Should().NotBeNullOrEmpty();

            var fieldCheckers = new Dictionary<string, Func<Project, bool>>(StringComparer.OrdinalIgnoreCase)
            {
                ["name"] = p => p.Snapshot.Name != null,
                ["full_name"] = p => p.Snapshot.FullName != null,
                ["description"] = p => p.Snapshot.Description != null,
                ["html_url"] = p => p.Snapshot.HtmlUrl != null,
                ["language"] = p => p.Snapshot.Language != null,
                ["topics"] = p => p.Snapshot.Topics != null,
                ["stargazers_count"] = p => p.Snapshot.StargazersCount >= 0,
                ["forks_count"] = p => p.Snapshot.ForksCount >= 0,
                ["open_issues_count"] = p => p.Snapshot.OpenIssuesCount >= 0,
                ["homepage"] = p => p.Snapshot.Homepage != null,
                ["default_branch"] = p => p.Snapshot.DefaultBranch != null,
                ["license"] = p => p.Snapshot.License != null,
                ["owner login"] = p => p.Snapshot.OwnerLogin != null,
                ["owner avatar url"] = p => p.Snapshot.OwnerAvatarUrl != null,
                ["created_at"] = p => p.Snapshot.CreatedAt != default,
                ["updated_at"] = p => p.Snapshot.UpdatedAt != default,
                ["pushed_at"] = p => p.Snapshot.PushedAt != default,
            };

            var fields = table.Rows.Select(row => row["Field"]).ToList();
            fields.Should().AllSatisfy(field => fieldCheckers.Should().ContainKey(field));

            _fixtures.ProjectRepository.Verify(x => x.CreateAsync(It.Is<Project>(p =>
                fields.All(field => fieldCheckers[field](p))
            )), Times.Once);
        }

        [Then("The full project details are returned")]
        public void ThenTheFullProjectDetailsAreReturned()
        {
            _context.LastException.Should().BeNull();
            _getProjectByIdResult.Should().NotBeNull();
            _getProjectByIdResult!.Name.Should().NotBeNullOrEmpty();
        }

        [Then("The project includes a link to the original GitHub repository")]
        public void ThenTheProjectIncludesALinkToTheOriginalGitHubRepository()
        {
            _getProjectByIdResult.Should().NotBeNull();
            _getProjectByIdResult!.GithubUrl.Should().NotBeNullOrEmpty();
        }
    }
}