using AwesomeAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Net;
using TimeForCode.Reporting.Api.V1.Controllers;
using TimeForCode.Reporting.Api.V1.Models;
using TimeForCode.Reporting.Commands;
using TimeForCode.Reporting.Values;

namespace TimeForCode.Reporting.Api.Tests
{
    [TestClass]
    public class BadgeControllerTests
    {
        private const string Id = "507f1f77bcf86cd799439011";
        private static readonly DateTimeOffset AwardedAt = new(2026, 11, 1, 9, 0, 0, TimeSpan.Zero);

        private readonly Mock<IMediator> _mediator = new();

        private BadgeController CreateSut() => new(_mediator.Object);

        private static int StatusOf(IActionResult result) => result.Should().BeAssignableTo<ObjectResult>().Subject.StatusCode!.Value;

        [TestMethod]
        public async Task GetOrganizationBadges_ReturnsNamesAndAwardDatesOnly()
        {
            _mediator.Setup(m => m.Send(It.Is<GetOrganizationBadgesQuery>(q => q.DonorOrganizationId == Id), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<IReadOnlyList<BadgeDto>>.Success([new BadgeDto { Name = "Bronze", AwardedAt = AwardedAt }]));

            var result = await CreateSut().GetOrganizationBadges(Id);

            var response = result.Should().BeOfType<OkObjectResult>().Subject.Value.Should().BeOfType<BadgesResponse>().Subject;
            response.Badges.Should().ContainSingle().Which.Should().BeEquivalentTo(new BadgeResponse { Name = "Bronze", AwardedAt = AwardedAt });
        }

        [TestMethod]
        public async Task GetOrganizationBadges_UnknownOrganization_ReturnsNotFound()
        {
            _mediator.Setup(m => m.Send(It.IsAny<GetOrganizationBadgesQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<IReadOnlyList<BadgeDto>>.Failure("Donor organization not found.", HttpStatusCode.NotFound));

            var result = await CreateSut().GetOrganizationBadges(Id);

            StatusOf(result).Should().Be(StatusCodes.Status404NotFound);
        }

        [TestMethod]
        public async Task GetContributorBadges_ReturnsTheBadges()
        {
            _mediator.Setup(m => m.Send(It.Is<GetContributorBadgesQuery>(q => q.ContributorId == Id), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<IReadOnlyList<BadgeDto>>.Success([new BadgeDto { Name = "Starter", AwardedAt = AwardedAt }]));

            var result = await CreateSut().GetContributorBadges(Id);

            result.Should().BeOfType<OkObjectResult>().Subject.Value.Should().BeOfType<BadgesResponse>().Subject.Badges.Should().ContainSingle();
        }

        [TestMethod]
        public async Task GetContributorBadges_DonationContextDown_ReturnsBadGateway()
        {
            _mediator.Setup(m => m.Send(It.IsAny<GetContributorBadgesQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<IReadOnlyList<BadgeDto>>.Failure("The Donation service could not be reached.", HttpStatusCode.BadGateway));

            var result = await CreateSut().GetContributorBadges(Id);

            StatusOf(result).Should().Be(StatusCodes.Status502BadGateway);
        }

        [TestMethod]
        public async Task GetPublicStatistics_ReturnsTheTotals()
        {
            _mediator.Setup(m => m.Send(It.IsAny<GetPublicStatisticsQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<PublicStatisticsResult>.Success(new PublicStatisticsResult { TotalHoursLogged = 20, OrganizationsSupported = 2 }));

            var result = await CreateSut().GetPublicStatistics();

            var response = result.Should().BeOfType<OkObjectResult>().Subject.Value.Should().BeOfType<PublicStatisticsResponse>().Subject;
            response.TotalHoursLogged.Should().Be(20);
            response.OrganizationsSupported.Should().Be(2);
        }

        [TestMethod]
        public async Task GetBadgeTiers_ReturnsTheTiers()
        {
            _mediator.Setup(m => m.Send(It.Is<GetBadgeTiersQuery>(q => q.RecipientType == BadgeRecipientType.Contributor), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<IReadOnlyList<BadgeTierDto>>.Success([new BadgeTierDto { Name = "Starter", RecipientType = BadgeRecipientType.Contributor, ThresholdHours = 5 }]));

            var result = await CreateSut().GetBadgeTiers(BadgeRecipientType.Contributor);

            var tier = result.Should().BeOfType<OkObjectResult>().Subject.Value.Should().BeOfType<BadgeTiersResponse>().Subject.Tiers.Should().ContainSingle().Subject;
            tier.Name.Should().Be("Starter");
            tier.RecipientType.Should().Be(BadgeRecipientType.Contributor);
            tier.ThresholdHours.Should().Be(5);
        }

        [TestMethod]
        public async Task DefineBadgeTier_Valid_ReturnsTheTier()
        {
            _mediator.Setup(m => m.Send(It.Is<DefineBadgeTierCommand>(c => c.Name == "Bronze" && c.RecipientType == BadgeRecipientType.Organization && c.ThresholdHours == 10), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<BadgeTierDto>.Success(new BadgeTierDto { Name = "Bronze", RecipientType = BadgeRecipientType.Organization, ThresholdHours = 10 }));

            var result = await CreateSut().DefineBadgeTier(BadgeRecipientType.Organization, "Bronze", new DefineBadgeTierRequest { ThresholdHours = 10 });

            result.Should().BeOfType<OkObjectResult>().Subject.Value.Should().BeOfType<BadgeTierResponse>().Subject.ThresholdHours.Should().Be(10);
        }

        [TestMethod]
        [DataRow(HttpStatusCode.BadRequest, StatusCodes.Status400BadRequest)]
        [DataRow(HttpStatusCode.Conflict, StatusCodes.Status409Conflict)]
        public async Task DefineBadgeTier_Refused_ReturnsTheMatchingStatus(HttpStatusCode failure, int expected)
        {
            _mediator.Setup(m => m.Send(It.IsAny<DefineBadgeTierCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<BadgeTierDto>.Failure("Refused.", failure));

            var result = await CreateSut().DefineBadgeTier(BadgeRecipientType.Organization, "Bronze", new DefineBadgeTierRequest { ThresholdHours = 0 });

            StatusOf(result).Should().Be(expected);
        }

        [TestMethod]
        public async Task RemoveBadgeTier_Existing_ReturnsNoContent()
        {
            _mediator.Setup(m => m.Send(It.Is<RemoveBadgeTierCommand>(c => c.Name == "Bronze" && c.RecipientType == BadgeRecipientType.Organization), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<bool>.Success(true));

            var result = await CreateSut().RemoveBadgeTier(BadgeRecipientType.Organization, "Bronze");

            result.Should().BeOfType<NoContentResult>();
        }

        [TestMethod]
        public async Task RemoveBadgeTier_Unknown_ReturnsNotFound()
        {
            _mediator.Setup(m => m.Send(It.IsAny<RemoveBadgeTierCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<bool>.Failure("Badge tier not found.", HttpStatusCode.NotFound));

            var result = await CreateSut().RemoveBadgeTier(BadgeRecipientType.Organization, "Bronze");

            StatusOf(result).Should().Be(StatusCodes.Status404NotFound);
        }
    }
}