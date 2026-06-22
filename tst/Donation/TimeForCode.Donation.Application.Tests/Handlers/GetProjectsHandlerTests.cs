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
    public class GetProjectsHandlerTests
    {
        private Mock<IProjectRepository> _mockRepository = default!;
        private GetProjectsHandler _sut = default!;

        [TestInitialize]
        public void Setup()
        {
            _mockRepository = new Mock<IProjectRepository>();
            _sut = new GetProjectsHandler(_mockRepository.Object, NullLogger<GetProjectsHandler>.Instance);
        }

        [TestMethod]
        public async Task Handle_PublishedProjectsExist_ReturnsSuccess()
        {
            var project = BuildPublishedProject();
            _mockRepository.Setup(r => r.GetAllPublishedAsync(1, 20))
                .ReturnsAsync(((IReadOnlyList<Project>)[project], 1));

            var query = new GetProjectsQuery { PageNumber = 1, PageSize = 20 };
            var result = await _sut.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Projects.Should().HaveCount(1);
            result.Value.TotalCount.Should().Be(1);
            result.Value.PageNumber.Should().Be(1);
            result.Value.PageSize.Should().Be(20);
        }

        [TestMethod]
        public async Task Handle_NoPublishedProjects_ReturnsSuccessWithEmptyList()
        {
            _mockRepository.Setup(r => r.GetAllPublishedAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(((IReadOnlyList<Project>)[], 0));

            var query = new GetProjectsQuery { PageNumber = 1, PageSize = 20 };
            var result = await _sut.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Projects.Should().BeEmpty();
            result.Value.TotalCount.Should().Be(0);
        }

        [TestMethod]
        public async Task Handle_PublishedProject_MapsAllFieldsCorrectly()
        {
            var project = BuildPublishedProject();
            _mockRepository.Setup(r => r.GetAllPublishedAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(((IReadOnlyList<Project>)[project], 1));

            var query = new GetProjectsQuery { PageNumber = 1, PageSize = 20 };
            var result = await _sut.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            var dto = result.Value.Projects[0];
            dto.Id.Should().Be(project.Id.ToString());
            dto.Name.Should().Be(project.Snapshot.Name);
            dto.FullName.Should().Be(project.Snapshot.FullName);
            dto.GithubUrl.Should().Be(project.Snapshot.HtmlUrl);
            dto.Language.Should().Be(project.Snapshot.Language);
            dto.Topics.Should().BeEquivalentTo(project.Snapshot.Topics);
            dto.StargazersCount.Should().Be(project.Snapshot.StargazersCount);
            dto.ForksCount.Should().Be(project.Snapshot.ForksCount);
            dto.OpenIssuesCount.Should().Be(project.Snapshot.OpenIssuesCount);
            dto.Status.Should().Be(project.Status);
        }

        [TestMethod]
        public async Task Handle_PaginationParameters_ArePassedToRepository()
        {
            _mockRepository.Setup(r => r.GetAllPublishedAsync(2, 10))
                .ReturnsAsync(((IReadOnlyList<Project>)[], 0));

            var query = new GetProjectsQuery { PageNumber = 2, PageSize = 10 };
            var result = await _sut.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.PageNumber.Should().Be(2);
            result.Value.PageSize.Should().Be(10);
            _mockRepository.Verify(r => r.GetAllPublishedAsync(2, 10), Times.Once);
        }

        private static Project BuildPublishedProject()
        {
            return new Project
            {
                Id = MongoDB.Bson.ObjectId.GenerateNewId(),
                Snapshot = new GithubSnapshot
                {
                    Name = "test-repo",
                    FullName = "owner/test-repo",
                    HtmlUrl = "https://github.com/owner/test-repo",
                    Language = "C#",
                    Topics = ["dotnet", "testing"],
                    StargazersCount = 42,
                    ForksCount = 5,
                    OpenIssuesCount = 3,
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