using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver;
using Moq;
using System.Net;
using System.Reflection;
using TimeForCode.Donation.Application.Exceptions;
using TimeForCode.Donation.Domain;
using TimeForCode.Donation.Domain.Entities;
using TimeForCode.Donation.Infrastructure.Persistence.Database;
using TimeForCode.Donation.Values;

namespace TimeForCode.Donation.Infrastructure.Tests.Persistence
{
    [TestClass]
    public class ProjectRepositoryTests
    {
        private Mock<IMongoDbContext> _mockContext = default!;
        private Mock<IMongoCollection<Project>> _mockCollection = default!;
        private Mock<IMongoIndexManager<Project>> _mockIndexManager = default!;

        [TestInitialize]
        public void Setup()
        {
            // Reset static index-creation flag to ensure each test starts with a clean state.
            typeof(ProjectRepository)
                .GetField("_publishedGithubUrlIndexCreated", BindingFlags.NonPublic | BindingFlags.Static)!
                .SetValue(null, false);

            _mockIndexManager = new Mock<IMongoIndexManager<Project>>();
            _mockIndexManager.Setup(m => m.CreateOne(
                    It.IsAny<CreateIndexModel<Project>>(),
                    It.IsAny<CreateOneIndexOptions>(),
                    It.IsAny<CancellationToken>()))
                .Returns(string.Empty);

            _mockCollection = new Mock<IMongoCollection<Project>>();
            _mockCollection.Setup(c => c.Indexes).Returns(_mockIndexManager.Object);

            _mockContext = new Mock<IMongoDbContext>();
            _mockContext.Setup(ctx => ctx.GetCollection<Project>()).Returns(_mockCollection.Object);
        }

        // ── GetByIdAsync ─────────────────────────────────────────────────────────

        [TestMethod]
        public async Task GetByIdAsync_InvalidObjectId_ReturnsNull()
        {
            var sut = new ProjectRepository(_mockContext.Object);

            var result = await sut.GetByIdAsync("not-a-valid-object-id");

            result.Should().BeNull();
        }

        [TestMethod]
        public async Task GetByIdAsync_ValidObjectIdProjectExists_ReturnsProject()
        {
            var project = BuildProject();
            SetupFindAsync([project]);
            var sut = new ProjectRepository(_mockContext.Object);

            var result = await sut.GetByIdAsync(project.Id.ToString());

            result.Should().NotBeNull();
            result!.Id.Should().Be(project.Id);
        }

        [TestMethod]
        public async Task GetByIdAsync_ValidObjectIdProjectNotFound_ReturnsNull()
        {
            SetupFindAsync([]);
            var sut = new ProjectRepository(_mockContext.Object);

            var result = await sut.GetByIdAsync(ObjectId.GenerateNewId().ToString());

            result.Should().BeNull();
        }

        // ── GetAllPublishedAsync ─────────────────────────────────────────────────

        [TestMethod]
        public async Task GetAllPublishedAsync_HasPublishedProjects_ReturnsProjectsWithCount()
        {
            var project = BuildProject();
            SetupFindAsync([project]);
            SetupCountDocumentsAsync(1L);
            var sut = new ProjectRepository(_mockContext.Object);

            var (projects, totalCount) = await sut.GetAllPublishedAsync(1, 20);

            projects.Should().HaveCount(1);
            projects[0].Id.Should().Be(project.Id);
            totalCount.Should().Be(1);
        }

        [TestMethod]
        public async Task GetAllPublishedAsync_NoPublishedProjects_ReturnsEmptyListWithZeroCount()
        {
            SetupFindAsync([]);
            SetupCountDocumentsAsync(0L);
            var sut = new ProjectRepository(_mockContext.Object);

            var (projects, totalCount) = await sut.GetAllPublishedAsync(1, 20);

            projects.Should().BeEmpty();
            totalCount.Should().Be(0);
        }

        // ── CreateAsync ──────────────────────────────────────────────────────────

        [TestMethod]
        public async Task CreateAsync_SuccessfulInsert_CompletesWithoutException()
        {
            var project = BuildProject();
            _mockCollection.Setup(c => c.InsertOneAsync(
                    It.IsAny<Project>(),
                    It.IsAny<InsertOneOptions>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            var sut = new ProjectRepository(_mockContext.Object);

            var act = async () => await sut.CreateAsync(project);

            await act.Should().NotThrowAsync();
        }

        [TestMethod]
        public async Task CreateAsync_DuplicateKeyException_ThrowsRepositoryConflictException()
        {
            var project = BuildProject();
            _mockCollection.Setup(c => c.InsertOneAsync(
                    It.IsAny<Project>(),
                    It.IsAny<InsertOneOptions>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(CreateDuplicateKeyException());
            var sut = new ProjectRepository(_mockContext.Object);

            var act = async () => await sut.CreateAsync(project);

            await act.Should().ThrowAsync<RepositoryConflictException>()
                .WithMessage("Repository is already published.");
        }

        // ── UpdateAsync ──────────────────────────────────────────────────────────

        [TestMethod]
        public async Task UpdateAsync_ValidProject_CompletesWithoutException()
        {
            var project = BuildProject();
            _mockCollection.Setup(c => c.UpdateOneAsync(
                    It.IsAny<FilterDefinition<Project>>(),
                    It.IsAny<UpdateDefinition<Project>>(),
                    It.IsAny<UpdateOptions>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new UpdateResult.Acknowledged(1, 1, null));
            var sut = new ProjectRepository(_mockContext.Object);

            var act = async () => await sut.UpdateAsync(project);

            await act.Should().NotThrowAsync();
        }

        // ── GetByGithubUrlAsync ──────────────────────────────────────────────────

        [TestMethod]
        public async Task GetByGithubUrlAsync_UrlExists_ReturnsProject()
        {
            var project = BuildProject();
            SetupFindAsync([project]);
            var sut = new ProjectRepository(_mockContext.Object);

            var result = await sut.GetByGithubUrlAsync(project.GithubRepositoryUrl);

            result.Should().NotBeNull();
            result!.Id.Should().Be(project.Id);
        }

        [TestMethod]
        public async Task GetByGithubUrlAsync_UrlDoesNotExist_ReturnsNull()
        {
            SetupFindAsync([]);
            var sut = new ProjectRepository(_mockContext.Object);

            var result = await sut.GetByGithubUrlAsync(new Uri("https://github.com/owner/nonexistent"));

            result.Should().BeNull();
        }

        // ── Helpers ──────────────────────────────────────────────────────────────

        private void SetupFindAsync(IEnumerable<Project> projects)
        {
            var projectList = projects.ToList();
            var mockCursor = new Mock<IAsyncCursor<Project>>();
            mockCursor.Setup(c => c.Current).Returns(projectList);
            mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(projectList.Count > 0)
                .ReturnsAsync(false);
            mockCursor.Setup(c => c.MoveNext(It.IsAny<CancellationToken>()))
                .Returns(false);

            _mockCollection.Setup(c => c.FindAsync(
                    It.IsAny<FilterDefinition<Project>>(),
                    It.IsAny<FindOptions<Project, Project>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockCursor.Object);
        }

        private void SetupCountDocumentsAsync(long count)
        {
            _mockCollection.Setup(c => c.CountDocumentsAsync(
                    It.IsAny<FilterDefinition<Project>>(),
                    It.IsAny<CountOptions>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(count);
        }

        private static Project BuildProject()
        {
            return new Project
            {
                Id = ObjectId.GenerateNewId(),
                Snapshot = new GithubSnapshot
                {
                    Name = "test-repo",
                    FullName = "owner/test-repo",
                    HtmlUrl = "https://github.com/owner/test-repo",
                    Topics = ["dotnet"],
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

        private static MongoWriteException CreateDuplicateKeyException()
        {
            var clusterId = new MongoDB.Driver.Core.Clusters.ClusterId(1);
            var serverId = new MongoDB.Driver.Core.Servers.ServerId(
                clusterId,
                new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, 27017));
            var connectionId = new MongoDB.Driver.Core.Connections.ConnectionId(serverId);

            // WriteError has an internal constructor in MongoDB.Driver 3.x — create via reflection.
            var writeError = (WriteError)typeof(WriteError)
                .GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)
                .Single()
                .Invoke([ServerErrorCategory.DuplicateKey, 11000, "E11000 duplicate key error", null]);

            return new MongoWriteException(connectionId, writeError, null, new Exception("Duplicate key"));
        }
    }
}