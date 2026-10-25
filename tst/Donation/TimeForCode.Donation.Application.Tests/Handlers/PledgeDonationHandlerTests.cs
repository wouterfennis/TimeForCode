using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Net;
using TimeForCode.Donation.Application.Handlers;
using TimeForCode.Donation.Application.Interfaces;
using TimeForCode.Donation.Commands;
using TimeForCode.Donation.Domain;
using TimeForCode.Donation.Values;
using DonationEntity = TimeForCode.Donation.Domain.Donation;

namespace TimeForCode.Donation.Application.Tests.Handlers
{
    [TestClass]
    public class PledgeDonationHandlerTests
    {
        private Mock<IProjectRepository> _projectRepository = default!;
        private Mock<IDonorOrganizationRepository> _organizationRepository = default!;
        private Mock<IDonationRepository> _donationRepository = default!;
        private PledgeDonationHandler _sut = default!;

        [TestInitialize]
        public void Setup()
        {
            _projectRepository = new Mock<IProjectRepository>();
            _organizationRepository = new Mock<IDonorOrganizationRepository>();
            _donationRepository = new Mock<IDonationRepository>();
            _sut = new PledgeDonationHandler(
                _projectRepository.Object,
                _organizationRepository.Object,
                _donationRepository.Object,
                NullLogger<PledgeDonationHandler>.Instance);
        }

        private static PledgeDonationCommand Command(string? organizationId = null) => new()
        {
            ProjectId = DonationTestData.ProjectId.ToString(),
            UserId = "user-1",
            Hours = 20,
            DonorOrganizationId = organizationId,
            AsIndividual = organizationId == null
        };

        private void GivenProject(ProjectStatus status)
        {
            _projectRepository.Setup(r => r.GetByIdAsync(DonationTestData.ProjectId.ToString()))
                .ReturnsAsync(ProjectTestData.Build(status, "owner"));
        }

        private void GivenOrganization()
        {
            _organizationRepository.Setup(r => r.GetByIdAsync(DonationTestData.OrganizationId.ToString()))
                .ReturnsAsync(new DonorOrganization
                {
                    Id = DonationTestData.OrganizationId,
                    Name = "Acme",
                    CreatedAt = DateTimeOffset.UtcNow
                });
        }

        [TestMethod]
        public async Task Handle_IndividualPledgeOnActiveProject_StoresPendingDonation()
        {
            GivenProject(ProjectStatus.Active);
            DonationEntity? stored = null;
            _donationRepository.Setup(r => r.CreateAsync(It.IsAny<DonationEntity>()))
                .Callback<DonationEntity>(d => stored = d)
                .Returns(Task.CompletedTask);

            var result = await _sut.Handle(Command(), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            stored.Should().NotBeNull();
            stored!.Status.Should().Be(DonationStatus.Pending);
            stored.DonorOrganizationId.Should().BeNull();
            stored.PledgedByUserId.Should().Be("user-1");
            stored.HoursCommitted.Should().Be(20);
            result.Value.Donation.Id.Should().Be(stored.Id.ToString());
        }

        [TestMethod]
        public async Task Handle_OrganizationPledge_LinksDonationToOrganization()
        {
            GivenProject(ProjectStatus.Active);
            GivenOrganization();
            DonationEntity? stored = null;
            _donationRepository.Setup(r => r.CreateAsync(It.IsAny<DonationEntity>()))
                .Callback<DonationEntity>(d => stored = d)
                .Returns(Task.CompletedTask);

            var result = await _sut.Handle(Command(DonationTestData.OrganizationId.ToString()), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            stored!.DonorOrganizationId.Should().Be(DonationTestData.OrganizationId);
        }

        [TestMethod]
        public async Task Handle_UnknownProject_ReturnsNotFound()
        {
            _projectRepository.Setup(r => r.GetByIdAsync(It.IsAny<string>())).ReturnsAsync((Project?)null);

            var result = await _sut.Handle(Command(), CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            result.FailureStatusCode.Should().Be(HttpStatusCode.NotFound);
            _donationRepository.Verify(r => r.CreateAsync(It.IsAny<DonationEntity>()), Times.Never);
        }

        [TestMethod]
        [DataRow(ProjectStatus.Draft)]
        [DataRow(ProjectStatus.PendingApproval)]
        [DataRow(ProjectStatus.Archived)]
        public async Task Handle_ProjectNotActive_ReturnsConflict(ProjectStatus status)
        {
            GivenProject(status);

            var result = await _sut.Handle(Command(), CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            result.FailureStatusCode.Should().Be(HttpStatusCode.Conflict);
            _donationRepository.Verify(r => r.CreateAsync(It.IsAny<DonationEntity>()), Times.Never);
        }

        [TestMethod]
        public async Task Handle_UnknownOrganization_ReturnsNotFound()
        {
            GivenProject(ProjectStatus.Active);
            _organizationRepository.Setup(r => r.GetByIdAsync(It.IsAny<string>())).ReturnsAsync((DonorOrganization?)null);

            var result = await _sut.Handle(Command(DonationTestData.OrganizationId.ToString()), CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            result.FailureStatusCode.Should().Be(HttpStatusCode.NotFound);
            _donationRepository.Verify(r => r.CreateAsync(It.IsAny<DonationEntity>()), Times.Never);
        }
    }
}