using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using Moq;
using TimeForCode.Donation.Application.Handlers;
using TimeForCode.Donation.Application.Interfaces;
using TimeForCode.Donation.Commands;
using TimeForCode.Donation.Values;
using DonationEntity = TimeForCode.Donation.Domain.Donation;

namespace TimeForCode.Donation.Application.Tests.Handlers
{
    [TestClass]
    public class GetMyProjectsHandlerTests
    {
        private const string FirstProjectId = "5f43a0e74b12c84f1b000001";
        private const string SecondProjectId = "5f43a0e74b12c84f1b000002";

        private Mock<IProjectRepository> _mockProjects = default!;
        private Mock<IDonationRepository> _mockDonations = default!;
        private GetMyProjectsHandler _sut = default!;

        [TestInitialize]
        public void Setup()
        {
            _mockProjects = new Mock<IProjectRepository>();
            _mockDonations = new Mock<IDonationRepository>();
            _mockDonations
                .Setup(r => r.GetReceivedByProjectIdsAsync(It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);
            _sut = new GetMyProjectsHandler(_mockProjects.Object, _mockDonations.Object, NullLogger<GetMyProjectsHandler>.Instance);
        }

        [TestMethod]
        public async Task Handle_UserHasProjectsInAnyState_ReturnsThemWithTheirStatus()
        {
            _mockProjects
                .Setup(r => r.GetByPublishedByUserIdAsync("user-1", It.IsAny<CancellationToken>()))
                .ReturnsAsync([
                    ProjectTestData.Build(ProjectStatus.Draft, "user-1", FirstProjectId),
                    ProjectTestData.Build(ProjectStatus.Active, "user-1", SecondProjectId)]);

            var result = await _sut.Handle(new GetMyProjectsQuery { UserId = "user-1" }, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Projects.Select(p => p.Project.Status).Should().Equal(ProjectStatus.Draft, ProjectStatus.Active);
        }

        [TestMethod]
        public async Task Handle_ProjectReceivedDonations_SumsTheHoursPerProject()
        {
            _mockProjects
                .Setup(r => r.GetByPublishedByUserIdAsync("user-1", It.IsAny<CancellationToken>()))
                .ReturnsAsync([
                    ProjectTestData.Build(ProjectStatus.Active, "user-1", FirstProjectId),
                    ProjectTestData.Build(ProjectStatus.Active, "user-1", SecondProjectId)]);
            _mockDonations
                .Setup(r => r.GetReceivedByProjectIdsAsync(It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([
                    DonationEntity.Create(new ObjectId(FirstProjectId), null, "donor-1", 10),
                    DonationEntity.Create(new ObjectId(FirstProjectId), null, "donor-2", 5),
                    DonationEntity.Create(new ObjectId(SecondProjectId), null, "donor-1", 7)]);

            var result = await _sut.Handle(new GetMyProjectsQuery { UserId = "user-1" }, CancellationToken.None);

            result.Value.Projects.Select(p => p.HoursDonated).Should().Equal(15, 7);
        }

        [TestMethod]
        public async Task Handle_ProjectWithoutDonations_ShowsZeroHours()
        {
            _mockProjects
                .Setup(r => r.GetByPublishedByUserIdAsync("user-1", It.IsAny<CancellationToken>()))
                .ReturnsAsync([ProjectTestData.Build(ProjectStatus.Draft, "user-1", FirstProjectId)]);

            var result = await _sut.Handle(new GetMyProjectsQuery { UserId = "user-1" }, CancellationToken.None);

            result.Value.Projects.Single().HoursDonated.Should().Be(0);
        }

        [TestMethod]
        public async Task Handle_UserHasNoProjects_ReturnsAnEmptyList()
        {
            _mockProjects
                .Setup(r => r.GetByPublishedByUserIdAsync("user-1", It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            var result = await _sut.Handle(new GetMyProjectsQuery { UserId = "user-1" }, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Projects.Should().BeEmpty();
        }
    }
}