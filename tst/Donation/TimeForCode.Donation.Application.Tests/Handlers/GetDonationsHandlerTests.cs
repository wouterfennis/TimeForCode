using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Net;
using TimeForCode.Donation.Application.Handlers;
using TimeForCode.Donation.Application.Interfaces;
using TimeForCode.Donation.Commands;
using TimeForCode.Donation.Domain;
using TimeForCode.Donation.Values;

namespace TimeForCode.Donation.Application.Tests.Handlers
{
    [TestClass]
    public class GetDonationsHandlerTests
    {
        private Mock<IDonationRepository> _donationRepository = default!;
        private Mock<IProjectRepository> _projectRepository = default!;

        [TestInitialize]
        public void Setup()
        {
            _donationRepository = new Mock<IDonationRepository>();
            _projectRepository = new Mock<IProjectRepository>();
        }

        [TestMethod]
        public async Task GetMyDonations_UserWithDonation_ReturnsProgress()
        {
            var donation = DonationTestData.Build(DonationStatus.Active, committed: 10, logged: 4);
            _donationRepository.Setup(r => r.GetByPledgedByUserIdAsync("user-1", It.IsAny<CancellationToken>()))
                .ReturnsAsync([donation]);
            var sut = new GetMyDonationsHandler(_donationRepository.Object, NullLogger<GetMyDonationsHandler>.Instance);

            var result = await sut.Handle(new GetMyDonationsQuery { UserId = "user-1" }, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            var dto = result.Value.Donations.Should().ContainSingle().Subject;
            dto.Status.Should().Be(DonationStatus.Active);
            dto.HoursCommitted.Should().Be(10);
            dto.HoursLogged.Should().Be(4);
            dto.ProgressPercentage.Should().Be(40);
        }

        [TestMethod]
        public async Task GetMyDonations_UserWithoutDonations_ReturnsEmptyList()
        {
            _donationRepository.Setup(r => r.GetByPledgedByUserIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);
            var sut = new GetMyDonationsHandler(_donationRepository.Object, NullLogger<GetMyDonationsHandler>.Instance);

            var result = await sut.Handle(new GetMyDonationsQuery { UserId = "user-1" }, CancellationToken.None);

            result.Value.Donations.Should().BeEmpty();
        }

        [TestMethod]
        public async Task GetProjectDonations_ActiveProject_ReturnsDonationsOfProject()
        {
            _projectRepository.Setup(r => r.GetByIdAsync(DonationTestData.ProjectId.ToString()))
                .ReturnsAsync(ProjectTestData.Build(ProjectStatus.Active, "owner"));
            _donationRepository.Setup(r => r.GetByProjectIdAsync(DonationTestData.ProjectId.ToString(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([DonationTestData.Build(DonationStatus.Pending), DonationTestData.Build(DonationStatus.Active)]);
            var sut = new GetProjectDonationsHandler(_projectRepository.Object, _donationRepository.Object, NullLogger<GetProjectDonationsHandler>.Instance);

            var result = await sut.Handle(new GetProjectDonationsQuery { ProjectId = DonationTestData.ProjectId.ToString() }, CancellationToken.None);

            result.Value.Donations.Should().HaveCount(2);
        }

        [TestMethod]
        public async Task GetProjectDonations_UnknownProject_ReturnsNotFound()
        {
            _projectRepository.Setup(r => r.GetByIdAsync(It.IsAny<string>())).ReturnsAsync((Project?)null);
            var sut = new GetProjectDonationsHandler(_projectRepository.Object, _donationRepository.Object, NullLogger<GetProjectDonationsHandler>.Instance);

            var result = await sut.Handle(new GetProjectDonationsQuery { ProjectId = DonationTestData.ProjectId.ToString() }, CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            result.FailureStatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task GetProjectDonations_ProjectNotActive_ReturnsNotFound()
        {
            _projectRepository.Setup(r => r.GetByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(ProjectTestData.Build(ProjectStatus.Draft, "owner"));
            var sut = new GetProjectDonationsHandler(_projectRepository.Object, _donationRepository.Object, NullLogger<GetProjectDonationsHandler>.Instance);

            var result = await sut.Handle(new GetProjectDonationsQuery { ProjectId = DonationTestData.ProjectId.ToString() }, CancellationToken.None);

            result.FailureStatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }
}