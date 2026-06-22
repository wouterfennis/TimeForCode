using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TimeForCode.Donation.Application.Handlers;
using TimeForCode.Donation.Application.Interfaces;
using TimeForCode.Donation.Commands;
using TimeForCode.Donation.Domain;
using TimeForCode.Donation.Values;

namespace TimeForCode.Donation.Application.Tests.Handlers
{
    [TestClass]
    public class UnpublishProjectHandlerTests
    {
        private Mock<IProjectRepository> _mockRepository = default!;
        private UnpublishProjectHandler _sut = default!;

        [TestInitialize]
        public void Setup()
        {
            _mockRepository = new Mock<IProjectRepository>();
            _sut = new UnpublishProjectHandler(_mockRepository.Object, NullLogger<UnpublishProjectHandler>.Instance);
        }

        [TestMethod]
        public async Task Handle_ProjectOwnedByUser_ReturnsSuccess()
        {
            var project = BuildPublishedProject(ownerId: "user-123");
            _mockRepository.Setup(r => r.GetByIdAsync(project.Id.ToString()))
                .ReturnsAsync(project);
            _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Project>())).Returns(Task.CompletedTask);

            var command = new UnpublishProjectCommand
            {
                ProjectId = project.Id.ToString(),
                UserId = "user-123"
            };

            var result = await _sut.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.ProjectId.Should().Be(project.Id.ToString());
        }

        [TestMethod]
        public async Task Handle_ProjectNotFound_ReturnsFailure()
        {
            _mockRepository.Setup(r => r.GetByIdAsync(It.IsAny<string>()))
                .ReturnsAsync((Project?)null);

            var command = new UnpublishProjectCommand
            {
                ProjectId = "5f43a0e74b12c84f1b000001",
                UserId = "user-123"
            };

            var result = await _sut.Handle(command, CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            result.ErrorMessage.Should().NotBeNullOrEmpty();
        }

        [TestMethod]
        public async Task Handle_ProjectOwnedByDifferentUser_ReturnsForbidden()
        {
            var project = BuildPublishedProject(ownerId: "other-user-456");
            _mockRepository.Setup(r => r.GetByIdAsync(project.Id.ToString()))
                .ReturnsAsync(project);

            var command = new UnpublishProjectCommand
            {
                ProjectId = project.Id.ToString(),
                UserId = "user-123"
            };

            var result = await _sut.Handle(command, CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            result.FailureStatusCode.Should().Be(System.Net.HttpStatusCode.Forbidden);
        }

        [TestMethod]
        public async Task Handle_ProjectOwnedByUser_SetsStatusToArchived()
        {
            var project = BuildPublishedProject(ownerId: "user-123");
            _mockRepository.Setup(r => r.GetByIdAsync(project.Id.ToString()))
                .ReturnsAsync(project);
            _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Project>())).Returns(Task.CompletedTask);

            var command = new UnpublishProjectCommand
            {
                ProjectId = project.Id.ToString(),
                UserId = "user-123"
            };

            await _sut.Handle(command, CancellationToken.None);

            _mockRepository.Verify(r => r.UpdateAsync(
                It.Is<Project>(p => p.Status == ProjectStatus.Archived)), Times.Once);
        }

        private static Project BuildPublishedProject(string ownerId)
        {
            return new Project
            {
                Id = new MongoDB.Bson.ObjectId("5f43a0e74b12c84f1b000001"),
                Snapshot = new GithubSnapshot
                {
                    Name = "test-repo",
                    FullName = "owner/test-repo",
                    HtmlUrl = "https://github.com/owner/test-repo",
                    Topics = [],
                    DefaultBranch = "main",
                    OwnerLogin = "owner",
                    OwnerAvatarUrl = "https://avatars.githubusercontent.com/u/1",
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow,
                    PushedAt = DateTimeOffset.UtcNow
                },
                GithubRepositoryUrl = new Uri("https://github.com/owner/test-repo"),
                Status = ProjectStatus.Published,
                PublishedByUserId = ownerId,
                PublishedAt = DateTimeOffset.UtcNow
            };
        }
    }
}