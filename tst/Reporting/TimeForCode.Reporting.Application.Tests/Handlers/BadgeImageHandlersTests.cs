using AwesomeAssertions;
using MediatR;
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
    public class BadgeImageHandlersTests
    {
        private const string Id = "507f1f77bcf86cd799439013";
        private static readonly DateTimeOffset Now = new(2026, 11, 1, 9, 0, 0, TimeSpan.Zero);

        private Mock<IMediator> _mediator = default!;
        private Mock<IBadgeTierRepository> _tiers = default!;

        [TestInitialize]
        public void Setup()
        {
            _mediator = new Mock<IMediator>();
            _tiers = new Mock<IBadgeTierRepository>();
        }

        private void GivenSummary(int hoursReceived, int hoursLogged, int progress) =>
            _mediator.Setup(m => m.Send(It.Is<GetProjectPublicSummaryQuery>(q => q.ProjectId == Id), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<ProjectPublicSummaryResult>.Success(new ProjectPublicSummaryResult
                {
                    HoursReceived = hoursReceived,
                    HoursLogged = hoursLogged,
                    ProgressPercentage = progress,
                    DonorCount = 2,
                    ContributorCount = 3
                }));

        private void GivenSummaryFails(HttpStatusCode statusCode) =>
            _mediator.Setup(m => m.Send(It.IsAny<GetProjectPublicSummaryQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<ProjectPublicSummaryResult>.Failure("failed", statusCode));

        private void GivenTiers(BadgeRecipientType type, params BadgeTier[] tiers) =>
            _tiers.Setup(t => t.ListAsync(type, It.IsAny<CancellationToken>())).ReturnsAsync(tiers);

        private static BadgeDto Badge(string name) => new() { Name = name, AwardedAt = Now };

        private void GivenOrganizationBadges(params string[] names) =>
            _mediator.Setup(m => m.Send(It.Is<GetOrganizationBadgesQuery>(q => q.DonorOrganizationId == Id), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<IReadOnlyList<BadgeDto>>.Success(names.Select(Badge).ToList()));

        private void GivenOrganizationBadgesFail(HttpStatusCode statusCode) =>
            _mediator.Setup(m => m.Send(It.IsAny<GetOrganizationBadgesQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<IReadOnlyList<BadgeDto>>.Failure("failed", statusCode));

        private void GivenContributorBadges(params string[] names) =>
            _mediator.Setup(m => m.Send(It.Is<GetContributorBadgesQuery>(q => q.ContributorId == Id), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<IReadOnlyList<BadgeDto>>.Success(names.Select(Badge).ToList()));

        private void GivenContributorBadgesFail(HttpStatusCode statusCode) =>
            _mediator.Setup(m => m.Send(It.IsAny<GetContributorBadgesQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<IReadOnlyList<BadgeDto>>.Failure("failed", statusCode));

        private GetProjectBadgeImageHandler ProjectHandler() => new(_mediator.Object);

        private GetOrganizationBadgeImageHandler OrganizationHandler() => new(_mediator.Object, _tiers.Object);

        private GetContributorBadgeImageHandler ContributorHandler() => new(_mediator.Object, _tiers.Object);

        // Project

        [TestMethod]
        public async Task Project_HoursReceived_ShowsTheHours()
        {
            GivenSummary(120, 45, 38);

            BadgeImageContent content = (await ProjectHandler().Handle(new GetProjectBadgeImageQuery { ProjectId = Id, Kind = ProjectBadgeKind.HoursReceived }, CancellationToken.None)).Value;

            content.Should().Be(new BadgeImageContent(BadgeImageState.HoursReceived, "120 hours"));
        }

        [TestMethod]
        public async Task Project_OneHourReceived_UsesTheSingularForm()
        {
            GivenSummary(1, 0, 0);

            BadgeImageContent content = (await ProjectHandler().Handle(new GetProjectBadgeImageQuery { ProjectId = Id, Kind = ProjectBadgeKind.HoursReceived }, CancellationToken.None)).Value;

            content.Text.Should().Be("1 hour");
        }

        [TestMethod]
        public async Task Project_Progress_ShowsHoursLoggedOutOfHoursReceivedWithThePercentage()
        {
            GivenSummary(120, 45, 38);

            BadgeImageContent content = (await ProjectHandler().Handle(new GetProjectBadgeImageQuery { ProjectId = Id, Kind = ProjectBadgeKind.Progress }, CancellationToken.None)).Value;

            content.Should().Be(new BadgeImageContent(BadgeImageState.Progress, "45 / 120 hours (38%)"));
        }

        [TestMethod]
        [DataRow(HttpStatusCode.NotFound)]
        [DataRow(HttpStatusCode.BadRequest)]
        public async Task Project_UnknownOrUnpublished_IsNotFound(HttpStatusCode statusCode)
        {
            GivenSummaryFails(statusCode);

            BadgeImageContent content = (await ProjectHandler().Handle(new GetProjectBadgeImageQuery { ProjectId = Id, Kind = ProjectBadgeKind.HoursReceived }, CancellationToken.None)).Value;

            content.State.Should().Be(BadgeImageState.NotFound);
        }

        [TestMethod]
        [DataRow(HttpStatusCode.BadGateway)]
        [DataRow(HttpStatusCode.InternalServerError)]
        public async Task Project_DonationDataFails_IsUnavailable(HttpStatusCode statusCode)
        {
            GivenSummaryFails(statusCode);

            BadgeImageContent content = (await ProjectHandler().Handle(new GetProjectBadgeImageQuery { ProjectId = Id, Kind = ProjectBadgeKind.HoursReceived }, CancellationToken.None)).Value;

            content.State.Should().Be(BadgeImageState.Unavailable);
        }

        // Organization

        [TestMethod]
        public async Task Organization_SeveralBadges_ShowsTheTierWithTheHighestThreshold()
        {
            GivenOrganizationBadges("Gold", "Silver");
            GivenTiers(
                BadgeRecipientType.Organization,
                new BadgeTier("Silver", BadgeRecipientType.Organization, 10),
                new BadgeTier("Gold", BadgeRecipientType.Organization, 20),
                new BadgeTier("Platinum", BadgeRecipientType.Organization, 50));

            BadgeImageContent content = (await OrganizationHandler().Handle(new GetOrganizationBadgeImageQuery { DonorOrganizationId = Id }, CancellationToken.None)).Value;

            content.Should().Be(new BadgeImageContent(BadgeImageState.Tier, "Gold"));
        }

        [TestMethod]
        public async Task Organization_TierWasRemoved_ShowsTheHeldBadge()
        {
            GivenOrganizationBadges("Silver");
            GivenTiers(BadgeRecipientType.Organization);

            BadgeImageContent content = (await OrganizationHandler().Handle(new GetOrganizationBadgeImageQuery { DonorOrganizationId = Id }, CancellationToken.None)).Value;

            content.Should().Be(new BadgeImageContent(BadgeImageState.Tier, "Silver"));
        }

        [TestMethod]
        public async Task Organization_NoBadges_IsNoBadgeYet()
        {
            GivenOrganizationBadges();
            GivenTiers(BadgeRecipientType.Organization, new BadgeTier("Silver", BadgeRecipientType.Organization, 10));

            BadgeImageContent content = (await OrganizationHandler().Handle(new GetOrganizationBadgeImageQuery { DonorOrganizationId = Id }, CancellationToken.None)).Value;

            content.State.Should().Be(BadgeImageState.NoBadgeYet);
        }

        [TestMethod]
        [DataRow(HttpStatusCode.NotFound)]
        [DataRow(HttpStatusCode.BadRequest)]
        public async Task Organization_Unknown_IsNotFound(HttpStatusCode statusCode)
        {
            GivenOrganizationBadgesFail(statusCode);

            BadgeImageContent content = (await OrganizationHandler().Handle(new GetOrganizationBadgeImageQuery { DonorOrganizationId = Id }, CancellationToken.None)).Value;

            content.State.Should().Be(BadgeImageState.NotFound);
        }

        [TestMethod]
        public async Task Organization_DonationDataFails_IsUnavailable()
        {
            GivenOrganizationBadgesFail(HttpStatusCode.BadGateway);

            BadgeImageContent content = (await OrganizationHandler().Handle(new GetOrganizationBadgeImageQuery { DonorOrganizationId = Id }, CancellationToken.None)).Value;

            content.State.Should().Be(BadgeImageState.Unavailable);
        }

        // Contributor

        [TestMethod]
        public async Task Contributor_SeveralBadges_ShowsTheTierWithTheHighestThreshold()
        {
            GivenContributorBadges("Bronze", "Silver");
            GivenTiers(
                BadgeRecipientType.Contributor,
                new BadgeTier("Bronze", BadgeRecipientType.Contributor, 10),
                new BadgeTier("Silver", BadgeRecipientType.Contributor, 20));

            BadgeImageContent content = (await ContributorHandler().Handle(new GetContributorBadgeImageQuery { ContributorId = Id }, CancellationToken.None)).Value;

            content.Should().Be(new BadgeImageContent(BadgeImageState.Tier, "Silver"));
        }

        [TestMethod]
        public async Task Contributor_NoBadges_IsNoBadgeYet()
        {
            GivenContributorBadges();
            GivenTiers(BadgeRecipientType.Contributor, new BadgeTier("Bronze", BadgeRecipientType.Contributor, 10));

            BadgeImageContent content = (await ContributorHandler().Handle(new GetContributorBadgeImageQuery { ContributorId = Id }, CancellationToken.None)).Value;

            content.State.Should().Be(BadgeImageState.NoBadgeYet);
        }

        [TestMethod]
        [DataRow(HttpStatusCode.NotFound)]
        [DataRow(HttpStatusCode.BadRequest)]
        public async Task Contributor_Unknown_IsTheSameAsAContributorWithoutBadges(HttpStatusCode statusCode)
        {
            GivenContributorBadgesFail(statusCode);

            BadgeImageContent content = (await ContributorHandler().Handle(new GetContributorBadgeImageQuery { ContributorId = Id }, CancellationToken.None)).Value;

            content.Should().Be(new BadgeImageContent(BadgeImageState.NoBadgeYet, null));
        }

        [TestMethod]
        public async Task Contributor_DonationDataFails_IsUnavailable()
        {
            GivenContributorBadgesFail(HttpStatusCode.BadGateway);

            BadgeImageContent content = (await ContributorHandler().Handle(new GetContributorBadgeImageQuery { ContributorId = Id }, CancellationToken.None)).Value;

            content.State.Should().Be(BadgeImageState.Unavailable);
        }
    }
}