using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Net;
using TimeForCode.Reporting.Application.Handlers;
using TimeForCode.Reporting.Application.Interfaces;
using TimeForCode.Reporting.Commands;
using TimeForCode.Reporting.Domain;
using TimeForCode.Reporting.Values;

namespace TimeForCode.Reporting.Application.Tests.Handlers
{
    [TestClass]
    public class BadgeHandlersTests
    {
        private const string RecipientId = "507f1f77bcf86cd799439013";
        private static readonly DateTimeOffset Now = new(2026, 11, 1, 9, 0, 0, TimeSpan.Zero);

        private Mock<IDonationDataSource> _donationData = default!;
        private Mock<IBadgeTierRepository> _tiers = default!;
        private Mock<IBadgeAwardRepository> _awards = default!;
        private BadgeAwarder _awarder = default!;

        [TestInitialize]
        public void Setup()
        {
            _donationData = new Mock<IDonationDataSource>();
            _tiers = new Mock<IBadgeTierRepository>();
            _awards = new Mock<IBadgeAwardRepository>();
            _awarder = new BadgeAwarder(_tiers.Object, _awards.Object, TimeProvider.System);
        }

        private GetOrganizationBadgesHandler OrganizationHandler() => new(_donationData.Object, _awarder, NullLogger<GetOrganizationBadgesHandler>.Instance);

        private GetContributorBadgesHandler ContributorHandler() => new(_donationData.Object, _awarder, NullLogger<GetContributorBadgesHandler>.Instance);

        private void GivenTiers(BadgeRecipientType type, params BadgeTier[] tiers) =>
            _tiers.Setup(t => t.ListAsync(type, It.IsAny<CancellationToken>())).ReturnsAsync(tiers);

        private void GivenAwarded(BadgeRecipientType type, params BadgeAward[] awards) =>
            _awards.Setup(a => a.ListAsync(type, RecipientId, It.IsAny<CancellationToken>())).ReturnsAsync(awards);

        [TestMethod]
        public async Task Organization_ReachedThresholds_AreAwardedAndListed()
        {
            _donationData.Setup(d => d.GetOrganizationHoursAsync(RecipientId, It.IsAny<CancellationToken>())).ReturnsAsync(Result<int>.Success(25));
            GivenTiers(
                BadgeRecipientType.Organization,
                new BadgeTier("Bronze", BadgeRecipientType.Organization, 10),
                new BadgeTier("Silver", BadgeRecipientType.Organization, 20),
                new BadgeTier("Gold", BadgeRecipientType.Organization, 50));
            GivenAwarded(BadgeRecipientType.Organization, new BadgeAward("Bronze", BadgeRecipientType.Organization, RecipientId, Now));

            var result = await OrganizationHandler().Handle(new GetOrganizationBadgesQuery { DonorOrganizationId = RecipientId }, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().ContainSingle().Which.Name.Should().Be("Bronze");
            _awards.Verify(a => a.AwardAsync(It.Is<BadgeAward>(b => b.TierName == "Bronze" && b.RecipientId == RecipientId && b.RecipientType == BadgeRecipientType.Organization), It.IsAny<CancellationToken>()), Times.Once);
            _awards.Verify(a => a.AwardAsync(It.Is<BadgeAward>(b => b.TierName == "Silver"), It.IsAny<CancellationToken>()), Times.Once);
            _awards.Verify(a => a.AwardAsync(It.Is<BadgeAward>(b => b.TierName == "Gold"), It.IsAny<CancellationToken>()), Times.Never);
        }

        [TestMethod]
        public async Task Organization_ExactlyAtTheThreshold_IsAwarded()
        {
            _donationData.Setup(d => d.GetOrganizationHoursAsync(RecipientId, It.IsAny<CancellationToken>())).ReturnsAsync(Result<int>.Success(10));
            GivenTiers(BadgeRecipientType.Organization, new BadgeTier("Bronze", BadgeRecipientType.Organization, 10));
            GivenAwarded(BadgeRecipientType.Organization);

            await OrganizationHandler().Handle(new GetOrganizationBadgesQuery { DonorOrganizationId = RecipientId }, CancellationToken.None);

            _awards.Verify(a => a.AwardAsync(It.Is<BadgeAward>(b => b.TierName == "Bronze"), It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public async Task Organization_BelowTheThreshold_HasNoBadges()
        {
            _donationData.Setup(d => d.GetOrganizationHoursAsync(RecipientId, It.IsAny<CancellationToken>())).ReturnsAsync(Result<int>.Success(9));
            GivenTiers(BadgeRecipientType.Organization, new BadgeTier("Bronze", BadgeRecipientType.Organization, 10));
            GivenAwarded(BadgeRecipientType.Organization);

            var result = await OrganizationHandler().Handle(new GetOrganizationBadgesQuery { DonorOrganizationId = RecipientId }, CancellationToken.None);

            result.Value.Should().BeEmpty();
            _awards.Verify(a => a.AwardAsync(It.IsAny<BadgeAward>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [TestMethod]
        public async Task Organization_Unknown_PassesOnTheFailure()
        {
            _donationData.Setup(d => d.GetOrganizationHoursAsync(RecipientId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<int>.Failure("Donor organization not found.", HttpStatusCode.NotFound));

            var result = await OrganizationHandler().Handle(new GetOrganizationBadgesQuery { DonorOrganizationId = RecipientId }, CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            result.FailureStatusCode.Should().Be(HttpStatusCode.NotFound);
            _awards.Verify(a => a.AwardAsync(It.IsAny<BadgeAward>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [TestMethod]
        public async Task Contributor_UsesOnlyContributorTiers()
        {
            _donationData.Setup(d => d.GetContributorHoursAsync(RecipientId, It.IsAny<CancellationToken>())).ReturnsAsync(Result<int>.Success(20));
            GivenTiers(BadgeRecipientType.Contributor, new BadgeTier("Starter", BadgeRecipientType.Contributor, 5));
            GivenAwarded(BadgeRecipientType.Contributor, new BadgeAward("Starter", BadgeRecipientType.Contributor, RecipientId, Now));

            var result = await ContributorHandler().Handle(new GetContributorBadgesQuery { ContributorId = RecipientId }, CancellationToken.None);

            result.Value.Should().ContainSingle().Which.Name.Should().Be("Starter");
            _tiers.Verify(t => t.ListAsync(BadgeRecipientType.Organization, It.IsAny<CancellationToken>()), Times.Never);
        }

        [TestMethod]
        public async Task Contributor_Unknown_PassesOnTheFailure()
        {
            _donationData.Setup(d => d.GetContributorHoursAsync(RecipientId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<int>.Failure("Contributor not found.", HttpStatusCode.NotFound));

            var result = await ContributorHandler().Handle(new GetContributorBadgesQuery { ContributorId = RecipientId }, CancellationToken.None);

            result.FailureStatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task PublicStatistics_ReturnsTheTotalsOfTheDonationContext()
        {
            _donationData.Setup(d => d.GetPlatformStatisticsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Result<PlatformStatistics>.Success(new PlatformStatistics(20, 2)));
            var sut = new GetPublicStatisticsHandler(_donationData.Object, NullLogger<GetPublicStatisticsHandler>.Instance);

            var result = await sut.Handle(new GetPublicStatisticsQuery(), CancellationToken.None);

            result.Value.TotalHoursLogged.Should().Be(20);
            result.Value.OrganizationsSupported.Should().Be(2);
        }

        [TestMethod]
        public async Task PublicStatistics_DonationContextFails_PassesOnTheFailure()
        {
            _donationData.Setup(d => d.GetPlatformStatisticsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<PlatformStatistics>.Failure("The Donation service could not be reached.", HttpStatusCode.BadGateway));
            var sut = new GetPublicStatisticsHandler(_donationData.Object, NullLogger<GetPublicStatisticsHandler>.Instance);

            var result = await sut.Handle(new GetPublicStatisticsQuery(), CancellationToken.None);

            result.FailureStatusCode.Should().Be(HttpStatusCode.BadGateway);
        }
    }
}