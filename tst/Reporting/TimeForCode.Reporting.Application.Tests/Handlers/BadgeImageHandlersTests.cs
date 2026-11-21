using AwesomeAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
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
        private const string Malformed = "not-an-identifier";
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

        private readonly RecordingLogger<GetProjectBadgeImageHandler> _projectLogger = new();
        private readonly RecordingLogger<GetOrganizationBadgeImageHandler> _organizationLogger = new();
        private readonly RecordingLogger<GetContributorBadgeImageHandler> _contributorLogger = new();

        private GetProjectBadgeImageHandler ProjectHandler() => new(_mediator.Object, _projectLogger);

        private GetOrganizationBadgeImageHandler OrganizationHandler() => new(_mediator.Object, _tiers.Object, _organizationLogger);

        private GetContributorBadgeImageHandler ContributorHandler() => new(_mediator.Object, _tiers.Object, _contributorLogger);

        /// <summary>Moq cannot proxy a logger of an internal type, so the warnings are recorded by hand.</summary>
        private sealed class RecordingLogger<T> : ILogger<T>
        {
            public int Warnings { get; private set; }

            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                if (logLevel == LogLevel.Warning)
                {
                    Warnings++;
                }
            }
        }

        // Unexpected failures: shown as "unavailable", but never silently

        [TestMethod]
        public async Task Project_DonationDataFails_LogsAWarning()
        {
            GivenSummaryFails(HttpStatusCode.BadGateway);

            await ProjectHandler().Handle(new GetProjectBadgeImageQuery { ProjectId = Id, Kind = ProjectBadgeKind.HoursReceived }, CancellationToken.None);

            _projectLogger.Warnings.Should().Be(1);
        }

        [TestMethod]
        public async Task Project_Unknown_LogsNoWarning()
        {
            GivenSummaryFails(HttpStatusCode.NotFound);

            await ProjectHandler().Handle(new GetProjectBadgeImageQuery { ProjectId = Id, Kind = ProjectBadgeKind.HoursReceived }, CancellationToken.None);

            _projectLogger.Warnings.Should().Be(0);
        }

        [TestMethod]
        public async Task Organization_DonationDataFails_LogsAWarning()
        {
            GivenOrganizationBadgesFail(HttpStatusCode.InternalServerError);

            await OrganizationHandler().Handle(new GetOrganizationBadgeImageQuery { DonorOrganizationId = Id }, CancellationToken.None);

            _organizationLogger.Warnings.Should().Be(1);
        }

        [TestMethod]
        public async Task Contributor_DonationDataFails_LogsAWarning()
        {
            GivenContributorBadgesFail(HttpStatusCode.BadGateway);

            await ContributorHandler().Handle(new GetContributorBadgeImageQuery { ContributorId = Id }, CancellationToken.None);

            _contributorLogger.Warnings.Should().Be(1);
        }

        [TestMethod]
        public async Task Contributor_Unknown_LogsNoWarning()
        {
            GivenContributorBadgesFail(HttpStatusCode.NotFound);

            await ContributorHandler().Handle(new GetContributorBadgeImageQuery { ContributorId = Id }, CancellationToken.None);

            _contributorLogger.Warnings.Should().Be(0);
        }

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
        public async Task Project_Progress_OverLogged_CapsThePercentageAtOneHundred()
        {
            GivenSummary(10, 12, 120);

            BadgeImageContent content = (await ProjectHandler().Handle(new GetProjectBadgeImageQuery { ProjectId = Id, Kind = ProjectBadgeKind.Progress }, CancellationToken.None)).Value;

            content.Should().Be(new BadgeImageContent(BadgeImageState.Progress, "12 / 10 hours (100%)"));
        }

        [TestMethod]
        public async Task Project_UnknownOrUnpublished_IsNotFound()
        {
            GivenSummaryFails(HttpStatusCode.NotFound);

            BadgeImageContent content = (await ProjectHandler().Handle(new GetProjectBadgeImageQuery { ProjectId = Id, Kind = ProjectBadgeKind.HoursReceived }, CancellationToken.None)).Value;

            content.State.Should().Be(BadgeImageState.NotFound);
        }

        [TestMethod]
        public async Task Project_MalformedIdentifier_IsNotFoundWithoutAskingDonation()
        {
            BadgeImageContent content = (await ProjectHandler().Handle(new GetProjectBadgeImageQuery { ProjectId = Malformed, Kind = ProjectBadgeKind.HoursReceived }, CancellationToken.None)).Value;

            content.State.Should().Be(BadgeImageState.NotFound);
            _mediator.VerifyNoOtherCalls();
        }

        [TestMethod]
        public async Task Project_DonationRejectsTheRequest_IsUnavailableAndLogsAWarning()
        {
            GivenSummaryFails(HttpStatusCode.BadRequest);

            BadgeImageContent content = (await ProjectHandler().Handle(new GetProjectBadgeImageQuery { ProjectId = Id, Kind = ProjectBadgeKind.HoursReceived }, CancellationToken.None)).Value;

            content.State.Should().Be(BadgeImageState.Unavailable);
            _projectLogger.Warnings.Should().Be(1);
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
        public async Task Organization_SameThreshold_ShowsTheSameTierWhateverTheOrderOfTheBadges()
        {
            GivenTiers(
                BadgeRecipientType.Organization,
                new BadgeTier("Silver", BadgeRecipientType.Organization, 10),
                new BadgeTier("Argent", BadgeRecipientType.Organization, 10));

            GivenOrganizationBadges("Silver", "Argent");
            BadgeImageContent first = (await OrganizationHandler().Handle(new GetOrganizationBadgeImageQuery { DonorOrganizationId = Id }, CancellationToken.None)).Value;
            GivenOrganizationBadges("Argent", "Silver");
            BadgeImageContent second = (await OrganizationHandler().Handle(new GetOrganizationBadgeImageQuery { DonorOrganizationId = Id }, CancellationToken.None)).Value;

            second.Should().Be(first);
        }

        [TestMethod]
        public async Task Organization_Unknown_IsNotFound()
        {
            GivenOrganizationBadgesFail(HttpStatusCode.NotFound);

            BadgeImageContent content = (await OrganizationHandler().Handle(new GetOrganizationBadgeImageQuery { DonorOrganizationId = Id }, CancellationToken.None)).Value;

            content.State.Should().Be(BadgeImageState.NotFound);
        }

        [TestMethod]
        public async Task Organization_MalformedIdentifier_IsNotFoundWithoutAskingDonation()
        {
            BadgeImageContent content = (await OrganizationHandler().Handle(new GetOrganizationBadgeImageQuery { DonorOrganizationId = Malformed }, CancellationToken.None)).Value;

            content.State.Should().Be(BadgeImageState.NotFound);
            _mediator.VerifyNoOtherCalls();
        }

        [TestMethod]
        public async Task Organization_DonationRejectsTheRequest_IsUnavailableAndLogsAWarning()
        {
            GivenOrganizationBadgesFail(HttpStatusCode.BadRequest);

            BadgeImageContent content = (await OrganizationHandler().Handle(new GetOrganizationBadgeImageQuery { DonorOrganizationId = Id }, CancellationToken.None)).Value;

            content.State.Should().Be(BadgeImageState.Unavailable);
            _organizationLogger.Warnings.Should().Be(1);
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
        public async Task Contributor_Unknown_IsTheSameAsAContributorWithoutBadges()
        {
            GivenContributorBadgesFail(HttpStatusCode.NotFound);

            BadgeImageContent content = (await ContributorHandler().Handle(new GetContributorBadgeImageQuery { ContributorId = Id }, CancellationToken.None)).Value;

            content.Should().Be(new BadgeImageContent(BadgeImageState.NoBadgeYet, null));
        }

        [TestMethod]
        public async Task Contributor_MalformedIdentifier_IsTheSameAsAContributorWithoutBadgesAndNotAskingDonation()
        {
            BadgeImageContent content = (await ContributorHandler().Handle(new GetContributorBadgeImageQuery { ContributorId = Malformed }, CancellationToken.None)).Value;

            content.Should().Be(new BadgeImageContent(BadgeImageState.NoBadgeYet, null));
            _mediator.VerifyNoOtherCalls();
        }

        [TestMethod]
        public async Task Contributor_DonationRejectsTheRequest_IsUnavailableAndLogsAWarning()
        {
            GivenContributorBadgesFail(HttpStatusCode.BadRequest);

            BadgeImageContent content = (await ContributorHandler().Handle(new GetContributorBadgeImageQuery { ContributorId = Id }, CancellationToken.None)).Value;

            content.State.Should().Be(BadgeImageState.Unavailable);
            _contributorLogger.Warnings.Should().Be(1);
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