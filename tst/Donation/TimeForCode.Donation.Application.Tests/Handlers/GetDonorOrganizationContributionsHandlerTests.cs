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
    public class GetDonorOrganizationContributionsHandlerTests
    {
        private static readonly ObjectId OrganizationId = new("5f43a0e74b12c84f1b0000d1");
        private static readonly ObjectId OtherOrganizationId = new("5f43a0e74b12c84f1b0000d2");

        private Mock<IDonorOrganizationRepository> _organizationRepository = default!;
        private Mock<IContributorRepository> _contributorRepository = default!;
        private Mock<IContributionRepository> _contributionRepository = default!;
        private GetDonorOrganizationContributionsHandler _sut = default!;

        [TestInitialize]
        public void Setup()
        {
            _organizationRepository = new Mock<IDonorOrganizationRepository>();
            _contributorRepository = new Mock<IContributorRepository>();
            _contributionRepository = new Mock<IContributionRepository>();
            _sut = new GetDonorOrganizationContributionsHandler(
                _organizationRepository.Object,
                _contributorRepository.Object,
                _contributionRepository.Object,
                NullLogger<GetDonorOrganizationContributionsHandler>.Instance);
        }

        private static GetDonorOrganizationContributionsQuery Query(bool isAdministrator = false) => new()
        {
            DonorOrganizationId = OrganizationId.ToString(),
            CallerUserId = "user-1",
            IsAdministrator = isAdministrator
        };

        private static Contributor AffiliatedContributor(string platformUserId, ObjectId? organizationId)
        {
            var contributor = Contributor.Create(platformUserId, "octocat", "https://avatars.githubusercontent.com/u/1");
            contributor.UpdateProfile(null, null, null, organizationId);
            return contributor;
        }

        private void GivenOrganization()
        {
            _organizationRepository.Setup(r => r.GetByIdAsync(OrganizationId.ToString()))
                .ReturnsAsync(DonorOrganization.Create("Acme", null, null));
        }

        private static Contribution Contribution(ObjectId contributorId, int hours) => new()
        {
            Id = ObjectId.GenerateNewId(),
            ProjectId = new ObjectId("5f43a0e74b12c84f1b000001"),
            ContributorId = contributorId,
            ContributorGitHubLogin = "octocat",
            ContributorAvatarUrl = "https://avatars.githubusercontent.com/u/1",
            Hours = hours,
            LoggedAt = new DateTimeOffset(2026, 10, 1, 9, 0, 0, TimeSpan.Zero)
        };

        [TestMethod]
        public async Task Handle_MemberOfTheOrganization_ReturnsTheContributionsOfItsContributors()
        {
            GivenOrganization();
            var caller = AffiliatedContributor("user-1", OrganizationId);
            _contributorRepository.Setup(r => r.GetByPlatformUserIdAsync("user-1")).ReturnsAsync(caller);
            _contributorRepository.Setup(r => r.GetByDonorOrganizationIdAsync(OrganizationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync([caller]);
            _contributionRepository
                .Setup(r => r.GetByContributorIdsAsync(
                    It.Is<IReadOnlyCollection<ObjectId>>(ids => ids.Single() == caller.Id),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync([Contribution(caller.Id, 4)]);

            var result = await _sut.Handle(Query(), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Contributions.Should().ContainSingle()
                .Which.Hours.Should().Be(4);
        }

        [TestMethod]
        public async Task Handle_AdministratorWithoutAffiliation_ReturnsTheContributions()
        {
            GivenOrganization();
            var member = AffiliatedContributor("user-2", OrganizationId);
            _contributorRepository.Setup(r => r.GetByDonorOrganizationIdAsync(OrganizationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync([member]);
            _contributionRepository
                .Setup(r => r.GetByContributorIdsAsync(It.IsAny<IReadOnlyCollection<ObjectId>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([Contribution(member.Id, 2)]);

            var result = await _sut.Handle(Query(isAdministrator: true), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Contributions.Should().ContainSingle();
        }

        [TestMethod]
        public async Task Handle_OrganizationWithoutContributors_ReturnsAnEmptyListWithoutQueryingContributions()
        {
            GivenOrganization();
            _contributorRepository.Setup(r => r.GetByDonorOrganizationIdAsync(OrganizationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            var result = await _sut.Handle(Query(isAdministrator: true), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Contributions.Should().BeEmpty();
            _contributionRepository.Verify(
                r => r.GetByContributorIdsAsync(It.IsAny<IReadOnlyCollection<ObjectId>>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [TestMethod]
        public async Task Handle_MemberOfAnotherOrganization_ReturnsForbidden()
        {
            GivenOrganization();
            _contributorRepository.Setup(r => r.GetByPlatformUserIdAsync("user-1"))
                .ReturnsAsync(AffiliatedContributor("user-1", OtherOrganizationId));

            var result = await _sut.Handle(Query(), CancellationToken.None);

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
        public async Task Handle_UnknownOrganization_ReturnsNotFound()
        {
            _organizationRepository.Setup(r => r.GetByIdAsync(OrganizationId.ToString()))
                .ReturnsAsync((DonorOrganization?)null);

            var result = await _sut.Handle(Query(), CancellationToken.None);

            result.FailureStatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }
}