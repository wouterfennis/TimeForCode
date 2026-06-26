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
    public class GetProjectByIdHandlerTests
    {
        private Mock<IProjectRepository> _mockRepository = default!;
        private GetProjectByIdHandler _sut = default!;

        [TestInitialize]
        public void Setup()
        {
            _mockRepository = new Mock<IProjectRepository>();
            _sut = new GetProjectByIdHandler(_mockRepository.Object, NullLogger<GetProjectByIdHandler>.Instance);
        }

        [TestMethod]
        public async Task Handle_PublishedProjectExists_ReturnsSuccess()
        {
            var project = BuildPublishedProject("5f43a0e74b12c84f1b000001");
            _mockRepository.Setup(r => r.GetByIdAsync(project.Id.ToString()))
                .ReturnsAsync(project);

            var query = new GetProjectByIdQuery { ProjectId = project.Id.ToString() };
            var result = await _sut.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Project.Id.Should().Be(project.Id.ToString());
            result.Value.Project.Name.Should().Be(project.Snapshot.Name);
        }

        [TestMethod]
        public async Task Handle_ProjectNotFound_ReturnsFailure()
        {
            _mockRepository.Setup(r => r.GetByIdAsync(It.IsAny<string>()))
                .ReturnsAsync((Project?)null);

            var query = new GetProjectByIdQuery { ProjectId = "5f43a0e74b12c84f1b000001" };
            var result = await _sut.Handle(query, CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            result.ErrorMessage.Should().NotBeNullOrEmpty();
        }

        [TestMethod]
        public async Task Handle_ArchivedProject_ReturnsFailure()
        {
            var project = BuildPublishedProject("5f43a0e74b12c84f1b000001");
            project.Status = ProjectStatus.Archived;
            _mockRepository.Setup(r => r.GetByIdAsync(project.Id.ToString()))
                .ReturnsAsync(project);

            var query = new GetProjectByIdQuery { ProjectId = project.Id.ToString() };
            var result = await _sut.Handle(query, CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            result.ErrorMessage.Should().NotBeNullOrEmpty();
        }

        [TestMethod]
        public async Task Handle_PublishedProject_MapsAllFieldsCorrectly()
        {
            var project = BuildPublishedProject("5f43a0e74b12c84f1b000001");
            _mockRepository.Setup(r => r.GetByIdAsync(project.Id.ToString()))
                .ReturnsAsync(project);

            var query = new GetProjectByIdQuery { ProjectId = project.Id.ToString() };
            var result = await _sut.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            var dto = result.Value.Project;
            dto.Id.Should().Be(project.Id.ToString());
            dto.Name.Should().Be(project.Snapshot.Name);
            dto.FullName.Should().Be(project.Snapshot.FullName);
            dto.GithubUrl.Should().Be(project.Snapshot.HtmlUrl);
            dto.Language.Should().Be(project.Snapshot.Language);
            dto.StargazersCount.Should().Be(project.Snapshot.StargazersCount);
            dto.Status.Should().Be(project.Status);
        }

        private static Project BuildPublishedProject(string id)
        {
            return new Project
            {
                Id = new MongoDB.Bson.ObjectId(id),
                Snapshot = new GithubSnapshot
                {
                    Name = "test-repo",
                    FullName = "owner/test-repo",
                    HtmlUrl = "https://github.com/owner/test-repo",
                    Language = "C#",
                    Topics = ["dotnet"],
                    StargazersCount = 10,
                    ForksCount = 2,
                    OpenIssuesCount = 1,
                    DefaultBranch = "main",
                    OwnerLogin = "owner",
                    OwnerAvatarUrl = "https://avatars.githubusercontent.com/u/1",
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow,
                    PushedAt = DateTimeOffset.UtcNow
                },
                GithubRepositoryUrl = new Uri("https://github.com/owner/test-repo"),
                Status = ProjectStatus.Published,
                PublishedByUserId = "user-123",
                PublishedAt = DateTimeOffset.UtcNow
            };
        }
    }
}