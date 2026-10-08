using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TimeForCode.Donation.Application.Exceptions;
using TimeForCode.Donation.Application.Handlers;
using TimeForCode.Donation.Application.Interfaces;
using TimeForCode.Donation.Commands;
using TimeForCode.Donation.Domain;
using TimeForCode.Donation.Values;

namespace TimeForCode.Donation.Application.Tests.Handlers
{
    [TestClass]
    public class RegisterProjectHandlerTests
    {
        private Mock<IGithubRepositoryApiService> _mockGithubService = default!;
        private Mock<IProjectRepository> _mockRepository = default!;
        private RegisterProjectHandler _sut = default!;

        [TestInitialize]
        public void Setup()
        {
            _mockGithubService = new Mock<IGithubRepositoryApiService>();
            _mockRepository = new Mock<IProjectRepository>();
            _sut = new RegisterProjectHandler(
                _mockGithubService.Object,
                _mockRepository.Object,
                NullLogger<RegisterProjectHandler>.Instance);
        }

        [TestMethod]
        public async Task Handle_ValidPublicRepository_ReturnsSuccess()
        {
            SetupGithubServiceSuccess(isPrivate: false, isArchived: false);
            _mockRepository.Setup(r => r.GetByGithubUrlAsync(It.IsAny<Uri>())).ReturnsAsync((Project?)null);
            _mockRepository.Setup(r => r.CreateAsync(It.IsAny<Project>())).Returns(Task.CompletedTask);

            var command = new RegisterProjectCommand
            {
                GithubRepositoryUrl = new Uri("https://github.com/owner/repo"),
                UserId = "user-123"
            };

            var result = await _sut.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.ProjectId.Should().NotBeNullOrEmpty();
        }

        [TestMethod]
        public async Task Handle_NonHttpsUrl_ReturnsFailure()
        {
            var command = new RegisterProjectCommand
            {
                GithubRepositoryUrl = new Uri("http://github.com/owner/repo"),
                UserId = "user-123"
            };

            var result = await _sut.Handle(command, CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            result.ErrorMessage.Should().NotBeNullOrEmpty();
        }

        [TestMethod]
        public async Task Handle_NonGithubUrl_ReturnsFailure()
        {
            var command = new RegisterProjectCommand
            {
                GithubRepositoryUrl = new Uri("https://gitlab.com/owner/repo"),
                UserId = "user-123"
            };

            var result = await _sut.Handle(command, CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            result.ErrorMessage.Should().NotBeNullOrEmpty();
        }

        [TestMethod]
        public async Task Handle_UrlWithTooManySegments_ReturnsFailure()
        {
            var command = new RegisterProjectCommand
            {
                GithubRepositoryUrl = new Uri("https://github.com/owner/repo/extra"),
                UserId = "user-123"
            };

            var result = await _sut.Handle(command, CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            result.ErrorMessage.Should().NotBeNullOrEmpty();
        }

        [TestMethod]
        public async Task Handle_GithubServiceReturnsFailure_ReturnsFailure()
        {
            _mockGithubService.Setup(s => s.GetRepositoryMetadataAsync(It.IsAny<Uri>()))
                .ReturnsAsync(Result<GithubSnapshot>.Failure("Repository not found."));

            var command = new RegisterProjectCommand
            {
                GithubRepositoryUrl = new Uri("https://github.com/owner/repo"),
                UserId = "user-123"
            };

            var result = await _sut.Handle(command, CancellationToken.None);

            result.IsFailure.Should().BeTrue();
        }

        [TestMethod]
        public async Task Handle_PrivateRepository_ReturnsFailure()
        {
            SetupGithubServiceSuccess(isPrivate: true, isArchived: false);

            var command = new RegisterProjectCommand
            {
                GithubRepositoryUrl = new Uri("https://github.com/owner/repo"),
                UserId = "user-123"
            };

            var result = await _sut.Handle(command, CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            result.ErrorMessage.Should().Contain("public");
        }

        [TestMethod]
        public async Task Handle_ArchivedRepository_ReturnsFailure()
        {
            SetupGithubServiceSuccess(isPrivate: false, isArchived: true);

            var command = new RegisterProjectCommand
            {
                GithubRepositoryUrl = new Uri("https://github.com/owner/repo"),
                UserId = "user-123"
            };

            var result = await _sut.Handle(command, CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            result.ErrorMessage.Should().Contain("archived");
        }

        [TestMethod]
        public async Task Handle_AlreadyPublishedRepository_ReturnsConflict()
        {
            SetupGithubServiceSuccess(isPrivate: false, isArchived: false);
            var existingProject = BuildProject(ProjectStatus.Active);
            _mockRepository.Setup(r => r.GetByGithubUrlAsync(It.IsAny<Uri>()))
                .ReturnsAsync(existingProject);

            var command = new RegisterProjectCommand
            {
                GithubRepositoryUrl = new Uri("https://github.com/owner/repo"),
                UserId = "user-123"
            };

            var result = await _sut.Handle(command, CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            result.FailureStatusCode.Should().Be(System.Net.HttpStatusCode.Conflict);
        }

        [TestMethod]
        [DataRow(ProjectStatus.Draft)]
        [DataRow(ProjectStatus.PendingApproval)]
        [DataRow(ProjectStatus.Archived)]
        public async Task Handle_RepositoryAlreadyRegisteredInAnyState_ReturnsConflict(ProjectStatus status)
        {
            SetupGithubServiceSuccess(isPrivate: false, isArchived: false);
            _mockRepository.Setup(r => r.GetByGithubUrlAsync(It.IsAny<Uri>()))
                .ReturnsAsync(BuildProject(status));

            var command = new RegisterProjectCommand
            {
                GithubRepositoryUrl = new Uri("https://github.com/owner/repo"),
                UserId = "user-123"
            };

            var result = await _sut.Handle(command, CancellationToken.None);

            result.FailureStatusCode.Should().Be(System.Net.HttpStatusCode.Conflict);
            _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Project>()), Times.Never);
        }

        [TestMethod]
        public async Task Handle_NewRepository_CreatesDraftProject()
        {
            SetupGithubServiceSuccess(isPrivate: false, isArchived: false);
            _mockRepository.Setup(r => r.GetByGithubUrlAsync(It.IsAny<Uri>())).ReturnsAsync((Project?)null);
            _mockRepository.Setup(r => r.CreateAsync(It.IsAny<Project>())).Returns(Task.CompletedTask);

            await _sut.Handle(new RegisterProjectCommand
            {
                GithubRepositoryUrl = new Uri("https://github.com/owner/repo"),
                UserId = "user-123"
            }, CancellationToken.None);

            _mockRepository.Verify(r => r.CreateAsync(It.Is<Project>(p => p.Status == ProjectStatus.Draft)), Times.Once);
        }

        [TestMethod]
        public async Task Handle_RepositoryConflictExceptionFromCreate_ReturnsConflict()
        {
            SetupGithubServiceSuccess(isPrivate: false, isArchived: false);
            _mockRepository.Setup(r => r.GetByGithubUrlAsync(It.IsAny<Uri>())).ReturnsAsync((Project?)null);
            _mockRepository.Setup(r => r.CreateAsync(It.IsAny<Project>()))
                .ThrowsAsync(new RepositoryConflictException("Repository is already published."));

            var command = new RegisterProjectCommand
            {
                GithubRepositoryUrl = new Uri("https://github.com/owner/repo"),
                UserId = "user-123"
            };

            var result = await _sut.Handle(command, CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            result.FailureStatusCode.Should().Be(System.Net.HttpStatusCode.Conflict);
        }

        private void SetupGithubServiceSuccess(bool isPrivate, bool isArchived)
        {
            var snapshot = new GithubSnapshot
            {
                Name = "repo",
                FullName = "owner/repo",
                HtmlUrl = "https://github.com/owner/repo",
                Topics = [],
                DefaultBranch = "main",
                OwnerLogin = "owner",
                OwnerAvatarUrl = "https://avatars.githubusercontent.com/u/1",
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
                PushedAt = DateTimeOffset.UtcNow,
                IsPrivate = isPrivate,
                IsArchived = isArchived
            };

            _mockGithubService.Setup(s => s.GetRepositoryMetadataAsync(It.IsAny<Uri>()))
                .ReturnsAsync(Result<GithubSnapshot>.Success(snapshot));
        }

        private static Project BuildProject(ProjectStatus status)
        {
            return new Project
            {
                Id = MongoDB.Bson.ObjectId.GenerateNewId(),
                Snapshot = new GithubSnapshot
                {
                    Name = "repo",
                    FullName = "owner/repo",
                    HtmlUrl = "https://github.com/owner/repo",
                    Topics = [],
                    DefaultBranch = "main",
                    OwnerLogin = "owner",
                    OwnerAvatarUrl = "https://avatars.githubusercontent.com/u/1",
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow,
                    PushedAt = DateTimeOffset.UtcNow
                },
                GithubRepositoryUrl = new Uri("https://github.com/owner/repo"),
                Status = status,
                PublishedByUserId = "user-123",
                PublishedAt = DateTimeOffset.UtcNow
            };
        }
    }
}