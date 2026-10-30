using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
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
    public class GetDonorOrganizationDonationsHandlerTests
    {
        private static readonly ObjectId OrganizationId = new("5f43a0e74b12c84f1b0000d1");
        private static readonly ObjectId OtherOrganizationId = new("5f43a0e74b12c84f1b0000d2");

        private Mock<IDonorOrganizationRepository> _organizationRepository = default!;
        private Mock<IContributorRepository> _contributorRepository = default!;
        private Mock<IDonationRepository> _donationRepository = default!;
        private GetDonorOrganizationDonationsHandler _sut = default!;

        [TestInitialize]
        public void Setup()
        {
            _organizationRepository = new Mock<IDonorOrganizationRepository>();
            _contributorRepository = new Mock<IContributorRepository>();
            _donationRepository = new Mock<IDonationRepository>();
            _sut = new GetDonorOrganizationDonationsHandler(
                _organizationRepository.Object,
                _contributorRepository.Object,
                _donationRepository.Object,
                NullLogger<GetDonorOrganizationDonationsHandler>.Instance);
        }

        private static GetDonorOrganizationDonationsQuery Query(bool isAdministrator = false) => new()
        {
            DonorOrganizationId = OrganizationId.ToString(),
            CallerUserId = "user-1",
            IsAdministrator = isAdministrator
        };

        private void GivenOrganization()
        {
            _organizationRepository.Setup(r => r.GetByIdAsync(OrganizationId.ToString()))
                .ReturnsAsync(DonorOrganization.Create("Acme", null, null));
        }

        private void GivenCallerAffiliatedWith(ObjectId? organizationId)
        {
            var contributor = Contributor.Create("user-1", "octocat", "https://avatars.githubusercontent.com/u/1");
            contributor.UpdateProfile(null, null, null, organizationId);
            _contributorRepository.Setup(r => r.GetByPlatformUserIdAsync("user-1")).ReturnsAsync(contributor);
        }

        private void GivenDonations(params DonationEntity[] donations)
        {
            _donationRepository
                .Setup(r => r.GetByDonorOrganizationIdsAsync(
                    It.Is<IReadOnlyCollection<string>>(ids => ids.Single() == OrganizationId.ToString()),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(donations);
        }

        [TestMethod]
        public async Task Handle_MemberOfTheOrganization_ReturnsItsDonations()
        {
            GivenOrganization();
            GivenCallerAffiliatedWith(OrganizationId);
            GivenDonations(DonationTestData.Build(DonationStatus.Active, committed: 10, logged: 4, organizationId: OrganizationId));

            var result = await _sut.Handle(Query(), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Donations.Should().ContainSingle()
                .Which.HoursLogged.Should().Be(4);
        }

        [TestMethod]
        public async Task Handle_AdministratorWithoutAffiliation_ReturnsItsDonations()
        {
            GivenOrganization();
            _contributorRepository.Setup(r => r.GetByPlatformUserIdAsync("user-1")).ReturnsAsync((Contributor?)null);
            GivenDonations(DonationTestData.Build(DonationStatus.Active, organizationId: OrganizationId));

            var result = await _sut.Handle(Query(isAdministrator: true), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Donations.Should().ContainSingle();
        }

        [TestMethod]
        public async Task Handle_MemberOfAnotherOrganization_ReturnsForbidden()
        {
            GivenOrganization();
            GivenCallerAffiliatedWith(OtherOrganizationId);

            var result = await _sut.Handle(Query(), CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.FailureStatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [TestMethod]
        public async Task Handle_CallerWithoutContributorProfile_ReturnsForbidden()
        {
            GivenOrganization();
            _contributorRepository.Setup(r => r.GetByPlatformUserIdAsync("user-1")).ReturnsAsync((Contributor?)null);

            var result = await _sut.Handle(Query(), CancellationToken.None);

            result.FailureStatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [TestMethod]
        public async Task Handle_OrganizationWithoutDonations_ReturnsAnEmptyList()
        {
            GivenOrganization();
            GivenCallerAffiliatedWith(OrganizationId);
            GivenDonations();

            var result = await _sut.Handle(Query(), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Donations.Should().BeEmpty();
        }

        [TestMethod]
        public async Task Handle_UnknownOrganization_ReturnsNotFound()
        {
            _organizationRepository.Setup(r => r.GetByIdAsync(OrganizationId.ToString()))
                .ReturnsAsync((DonorOrganization?)null);

            var result = await _sut.Handle(Query(), CancellationToken.None);

            result.FailureStatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }
}