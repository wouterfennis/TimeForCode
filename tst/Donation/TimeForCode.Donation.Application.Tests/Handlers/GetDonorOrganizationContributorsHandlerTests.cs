using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using Moq;
using System.Net;
using TimeForCode.Donation.Application.Handlers;
using TimeForCode.Donation.Application.Interfaces;
using TimeForCode.Donation.Commands;
using TimeForCode.Donation.Domain;

namespace TimeForCode.Donation.Application.Tests.Handlers
{
    [TestClass]
    public class GetDonorOrganizationContributorsHandlerTests
    {
        private static readonly ObjectId OrganizationId = new("5f43a0e74b12c84f1b0000d1");

        private Mock<IContributorRepository> _contributorRepository = default!;
        private Mock<IDonorOrganizationRepository> _organizationRepository = default!;
        private GetDonorOrganizationContributorsHandler _sut = default!;

        [TestInitialize]
        public void Setup()
        {
            _contributorRepository = new Mock<IContributorRepository>();
            _organizationRepository = new Mock<IDonorOrganizationRepository>();
            _sut = new GetDonorOrganizationContributorsHandler(
                _contributorRepository.Object,
                _organizationRepository.Object,
                NullLogger<GetDonorOrganizationContributorsHandler>.Instance);
        }

        [TestMethod]
        public async Task Handle_ExistingOrganization_ReturnsItsContributors()
        {
            _organizationRepository.Setup(r => r.GetByIdAsync(OrganizationId.ToString()))
                .ReturnsAsync(DonorOrganization.Create("Acme", null, null));
            var contributor = Contributor.Create("user-1", "octocat", "https://avatars.githubusercontent.com/u/1");
            contributor.UpdateProfile("Designer", null, null, OrganizationId);
            _contributorRepository.Setup(r => r.GetByDonorOrganizationIdAsync(OrganizationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync([contributor]);

            var result = await _sut.Handle(
                new GetDonorOrganizationContributorsQuery { DonorOrganizationId = OrganizationId.ToString() },
                CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Contributors.Should().ContainSingle()
                .Which.DonorOrganizationId.Should().Be(OrganizationId.ToString());
        }

        [TestMethod]
        public async Task Handle_OrganizationWithoutContributors_ReturnsAnEmptyList()
        {
            _organizationRepository.Setup(r => r.GetByIdAsync(OrganizationId.ToString()))
                .ReturnsAsync(DonorOrganization.Create("Acme", null, null));
            _contributorRepository.Setup(r => r.GetByDonorOrganizationIdAsync(OrganizationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            var result = await _sut.Handle(
                new GetDonorOrganizationContributorsQuery { DonorOrganizationId = OrganizationId.ToString() },
                CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Contributors.Should().BeEmpty();
        }

        [TestMethod]
        public async Task Handle_UnknownOrganization_ReturnsNotFound()
        {
            _organizationRepository.Setup(r => r.GetByIdAsync(OrganizationId.ToString()))
                .ReturnsAsync((DonorOrganization?)null);

            var result = await _sut.Handle(
                new GetDonorOrganizationContributorsQuery { DonorOrganizationId = OrganizationId.ToString() },
                CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            result.FailureStatusCode.Should().Be(HttpStatusCode.NotFound);
            _contributorRepository.Verify(r => r.GetByDonorOrganizationIdAsync(It.IsAny<ObjectId>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}