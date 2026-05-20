using FluentAssertions;
using Microsoft.AspNetCore.Http;
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
    [Binding]
    internal class ProjectSteps
    {
        private readonly IDonationClient _donationClient;
        private readonly IServiceProvider _provider;

        private TimeForCode.Donation.Api.Client.RegisterProjectResult? _registerResult;
        private GetProjectsResponse? _getProjectsResult;
        private ProjectResponse? _getProjectByIdResult;
        private ApiException? _exception;
        private string? _registeredProjectId;

        public ProjectSteps(IDonationClient donationClient, IServiceProvider provider)
        {
            _donationClient = donationClient;
            _provider = provider;
        }

        [Given("There is a project published by another user")]
        public void GivenThereIsAProjectPublishedByAnotherUser()
        {
            var anotherUsersProject = new Project
            {
                Id = new ObjectId(Constants.TestProjectId),
                Snapshot = ProjectBuilder.BuildSnapshot(),
                GithubRepositoryUrl = new Uri(Constants.TestGithubRepositoryUrl),
                Status = TimeForCode.Donation.Values.ProjectStatus.Published,
                PublishedByUserId = "another-user-id",
                PublishedAt = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero)
            };

            var mockProjectRepository = _provider.GetRequiredService<Mock<IProjectRepository>>();
            mockProjectRepository.Setup(x => x.GetByIdAsync(anotherUsersProject.Id.ToString()))
                .ReturnsAsync(anotherUsersProject);

            _registeredProjectId = anotherUsersProject.Id.ToString();
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

        [Given("The user has previously unpublished the repository on the time for code platform")]
        public void GivenTheUserHasPreviouslyUnpublishedTheRepositoryOnTheTimeForCodePlatform()
        {
            var archivedProject = new Project
            {
                Id = new ObjectId(Constants.TestProjectId),
                Snapshot = ProjectBuilder.BuildSnapshot(),
                GithubRepositoryUrl = new Uri(Constants.TestGithubRepositoryUrl),
                Status = TimeForCode.Donation.Values.ProjectStatus.Archived,
                PublishedByUserId = Constants.TestUserId,
                PublishedAt = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero)
            };

            var mockProjectRepository = _provider.GetRequiredService<Mock<IProjectRepository>>();
            mockProjectRepository.Setup(x => x.GetByGithubUrlAsync(It.IsAny<Uri>()))
                .ReturnsAsync(archivedProject);
            mockProjectRepository.Setup(x => x.UpdateAsync(It.IsAny<Project>()))
                .Returns(Task.CompletedTask);
        }

        [Given("The user has already published the repository on the time for code platform")]
        public void GivenTheUserHasAlreadyPublishedTheRepositoryOnTheTimeForCodePlatform()
        {
            var existingProject = ProjectBuilder.BuildPublished();

            var mockProjectRepository = _provider.GetRequiredService<Mock<IProjectRepository>>();
            mockProjectRepository.Setup(x => x.GetByGithubUrlAsync(It.IsAny<Uri>()))
                .ReturnsAsync(existingProject);
            mockProjectRepository.Setup(x => x.GetByIdAsync(existingProject.Id.ToString()))
                .ReturnsAsync(existingProject);

            var mockGithubService = _provider.GetRequiredService<Mock<IGithubRepositoryApiService>>();
            mockGithubService.Setup(x => x.GetRepositoryMetadataAsync(It.IsAny<Uri>()))
                .ReturnsAsync(Result<GithubSnapshot>.Success(ProjectBuilder.BuildSnapshot()));

            _registeredProjectId = existingProject.Id.ToString();
        }

        [Given("There are published projects on the time for code platform")]
        public void GivenThereArePublishedProjectsOnTheTimeForCodePlatform()
        {
            var project = ProjectBuilder.BuildPublished();
            var mockProjectRepository = _provider.GetRequiredService<Mock<IProjectRepository>>();
            mockProjectRepository.Setup(x => x.GetAllPublishedAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(((IReadOnlyList<Project>)new List<Project> { project }, 1));
        }

        [Given("There is a published project on the time for code platform")]
        public void GivenThereIsAPublishedProjectOnTheTimeForCodePlatform()
        {
            var project = ProjectBuilder.BuildPublished();
            var mockProjectRepository = _provider.GetRequiredService<Mock<IProjectRepository>>();
            mockProjectRepository.Setup(x => x.GetByIdAsync(project.Id.ToString()))
                .ReturnsAsync(project);
            mockProjectRepository.Setup(x => x.GetAllPublishedAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(((IReadOnlyList<Project>)new List<Project> { project }, 1));

            _registeredProjectId = project.Id.ToString();
        }

        [Given("One of the projects has been unpublished")]
        public void GivenOneOfTheProjectsHasBeenUnpublished()
        {
            // The published listing already excludes archived projects via the repository mock
            // Override to return empty list (simulating the archived project is filtered out)
            var mockProjectRepository = _provider.GetRequiredService<Mock<IProjectRepository>>();
            mockProjectRepository.Setup(x => x.GetAllPublishedAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(((IReadOnlyList<Project>)new List<Project>(), 0));
        }

        [When("The user publishes the repository on the time for code platform")]
        public async Task WhenTheUserPublishesTheRepositoryOnTheTimeForCodePlatformAsync()
        {
            var mockProjectRepository = _provider.GetRequiredService<Mock<IProjectRepository>>();
            mockProjectRepository.Setup(x => x.CreateAsync(It.IsAny<Project>()))
                .Callback<Project>(p => _registeredProjectId = p.Id.ToString())
                .Returns(Task.CompletedTask);

            try
            {
                _registerResult = await _donationClient.RegisterProjectAsync(new RegisterProjectRequest
                {
                    GithubRepositoryUrl = new Uri(Constants.TestGithubRepositoryUrl)
                });
            }
            catch (ApiException<ProblemDetails> exception)
            {
                _exception = exception;
            }
            catch (ApiException exception)
            {
                _exception = exception;
            }
        }

        [When("The user unpublishes the project on the time for code platform")]
        public async Task WhenTheUserUnpublishesTheProjectOnTheTimeForCodePlatformAsync()
        {
            var projectId = _registeredProjectId ?? Constants.TestProjectId;

            var mockProjectRepository = _provider.GetRequiredService<Mock<IProjectRepository>>();
            mockProjectRepository.Setup(x => x.UpdateAsync(It.IsAny<Project>()))
                .Callback<Project>(_ =>
                {
                    mockProjectRepository.Setup(x => x.GetAllPublishedAsync(It.IsAny<int>(), It.IsAny<int>()))
                        .ReturnsAsync(((IReadOnlyList<Project>)new List<Project>(), 0));
                })
                .Returns(Task.CompletedTask);

            try
            {
                await _donationClient.UnpublishProjectAsync(projectId);
            }
            catch (ApiException<ProblemDetails> exception)
            {
                _exception = exception;
            }
            catch (ApiException exception)
            {
                _exception = exception;
            }
        }

        [When("The user requests the list of published projects")]
        [When("The user requests the list of published projects without an account")]
        public async Task WhenTheUserRequestsTheListOfPublishedProjectsAsync()
        {
            try
            {
                _getProjectsResult = await _donationClient.GetProjectsAsync(null, null);
            }
            catch (ApiException<ProblemDetails> exception)
            {
                _exception = exception;
            }
            catch (ApiException exception)
            {
                _exception = exception;
            }
        }

        [When("The user requests the project details")]
        [When("The user requests the project details without an account")]
        public async Task WhenTheUserRequestsTheProjectDetailsAsync()
        {
            var projectId = _registeredProjectId ?? Constants.TestProjectId;

            try
            {
                _getProjectByIdResult = await _donationClient.GetProjectByIdAsync(projectId);
            }
            catch (ApiException<ProblemDetails> exception)
            {
                _exception = exception;
            }
            catch (ApiException exception)
            {
                _exception = exception;
            }
        }

        [Then("The project is registered on the time for code platform")]
        public void ThenTheProjectIsRegisteredOnTheTimeForCodePlatform()
        {
            _exception.Should().BeNull();
            _registerResult.Should().NotBeNull();
            _registerResult!.ProjectId.Should().NotBeNullOrEmpty();
        }

        [Then("The stored project metadata includes the following fields:")]
        public void ThenTheStoredProjectMetadataIncludesTheFollowingFields(DataTable table)
        {
            _exception.Should().BeNull();
            _registerResult.Should().NotBeNull();
            _registerResult!.ProjectId.Should().NotBeNullOrEmpty();

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

            var mockProjectRepository = _provider.GetRequiredService<Mock<IProjectRepository>>();
            mockProjectRepository.Verify(x => x.CreateAsync(It.Is<Project>(p =>
                fields.All(field => fieldCheckers[field](p))
            )), Times.Once);
        }

        [Then("The user is informed the repository cannot be published")]
        public void ThenTheUserIsInformedTheRepositoryCannotBePublished()
        {
            _exception.Should().NotBeNull();
            _exception!.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        }

        [Then("The user is informed the repository is already published")]
        public void ThenTheUserIsInformedTheRepositoryIsAlreadyPublished()
        {
            _exception.Should().NotBeNull();
            _exception!.StatusCode.Should().Be(StatusCodes.Status409Conflict);
        }

        [Then("The user is informed they do not have permission")]
        public void ThenTheUserIsInformedTheyDoNotHavePermission()
        {
            _exception.Should().NotBeNull();
            _exception!.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        }

        [Then("The user is informed they must be logged in")]
        public void ThenTheUserIsInformedTheyMustBeLoggedIn()
        {
            _exception.Should().NotBeNull();
            _exception!.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        }

        [Then("The project is archived on the time for code platform")]
        public void ThenTheProjectIsArchivedOnTheTimeForCodePlatform()
        {
            _exception.Should().BeNull();

            var mockProjectRepository = _provider.GetRequiredService<Mock<IProjectRepository>>();
            mockProjectRepository.Verify(x => x.UpdateAsync(It.Is<Project>(p =>
                p.Status == TimeForCode.Donation.Values.ProjectStatus.Archived
            )), Times.Once);
        }

        [Then("The project no longer appears in the public project listing")]
        public async Task ThenTheProjectNoLongerAppearsInThePublicProjectListingAsync()
        {
            var projectId = _registeredProjectId ?? Constants.TestProjectId;

            var result = await _donationClient.GetProjectsAsync(null, null);
            result.Projects.Should().NotContain(p => p.Id == projectId);
        }

        [Then("A paginated list of projects is returned")]
        public void ThenAPaginatedListOfProjectsIsReturned()
        {
            _exception.Should().BeNull();
            _getProjectsResult.Should().NotBeNull();
            _getProjectsResult!.Projects.Should().NotBeNull();
        }

        [Then("Each project includes a link to the original GitHub repository")]
        public void ThenEachProjectIncludesALinkToTheOriginalGitHubRepository()
        {
            _getProjectsResult.Should().NotBeNull();
            _getProjectsResult!.Projects.Should().NotBeEmpty();
            _getProjectsResult!.Projects.Should().AllSatisfy(p => p.GithubUrl.Should().NotBeNullOrEmpty());
        }

        [Then("The full project details are returned")]
        public void ThenTheFullProjectDetailsAreReturned()
        {
            _exception.Should().BeNull();
            _getProjectByIdResult.Should().NotBeNull();
            _getProjectByIdResult!.Name.Should().NotBeNullOrEmpty();
        }

        [Then("The project includes a link to the original GitHub repository")]
        public void ThenTheProjectIncludesALinkToTheOriginalGitHubRepository()
        {
            _getProjectByIdResult.Should().NotBeNull();
            _getProjectByIdResult!.GithubUrl.Should().NotBeNullOrEmpty();
        }

        [Then("The archived project is not included in the results")]
        public void ThenTheArchivedProjectIsNotIncludedInTheResults()
        {
            _exception.Should().BeNull();
            _getProjectsResult.Should().NotBeNull();
            _getProjectsResult!.Projects.Should().BeEmpty();
        }
    }
}
