using AwesomeAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using TimeForCode.Donation.Api.V1.Controllers;
using TimeForCode.Donation.Api.V1.Models;
using TimeForCode.Donation.Commands;

namespace TimeForCode.Donation.Api.Tests
{
    [TestClass]
    public class ContributionControllerTests
    {
        private const string UserId = "user-1";
        private const string OrganizationId = "507f1f77bcf86cd799439011";

        private readonly Mock<IMediator> _mediator = new();

        private ContributionController CreateSut(bool withIdentity = true, bool admin = false)
        {
            var claims = new List<Claim>();
            if (withIdentity)
            {
                claims.Add(new Claim(ClaimTypes.NameIdentifier, UserId));
            }
            if (admin)
            {
                claims.Add(new Claim("scope", "admin"));
            }

            return new ContributionController(_mediator.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test")) }
                }
            };
        }

        private static ContributionDto Contribution() => new()
        {
            Id = "507f1f77bcf86cd799439012",
            ProjectId = "507f1f77bcf86cd799439013",
            ContributorId = "507f1f77bcf86cd799439014",
            ContributorGitHubLogin = "octocat",
            ContributorAvatarUrl = "https://avatars.githubusercontent.com/u/1",
            Hours = 4,
            LoggedAt = new DateTimeOffset(2026, 10, 1, 9, 0, 0, TimeSpan.Zero)
        };

        [TestMethod]
        public async Task GetDonorOrganizationContributions_NoIdentity_ReturnsBadRequest()
        {
            var result = await CreateSut(withIdentity: false).GetDonorOrganizationContributions(OrganizationId);

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [TestMethod]
        public async Task GetDonorOrganizationContributions_Allowed_ReturnsTheContributions()
        {
            _mediator.Setup(m => m.Send(
                    It.Is<GetDonorOrganizationContributionsQuery>(q => q.DonorOrganizationId == OrganizationId && q.CallerUserId == UserId && !q.IsAdministrator),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<GetDonorOrganizationContributionsResult>.Success(Page([Contribution()], totalCount: 1)));

            var result = await CreateSut().GetDonorOrganizationContributions(OrganizationId);

            result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeOfType<GetDonorOrganizationContributionsResponse>()
                .Which.Contributions.Should().ContainSingle().Which.Hours.Should().Be(4);
        }

        [TestMethod]
        public async Task GetDonorOrganizationContributions_PageRequested_PassesThePageAndReturnsThePagingFields()
        {
            _mediator.Setup(m => m.Send(
                    It.Is<GetDonorOrganizationContributionsQuery>(q => q.PageNumber == 3 && q.PageSize == 5),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<GetDonorOrganizationContributionsResult>.Success(Page([Contribution()], totalCount: 11, pageNumber: 3, pageSize: 5)));

            var result = await CreateSut().GetDonorOrganizationContributions(OrganizationId, pageNumber: 3, pageSize: 5);

            result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeEquivalentTo(new { TotalCount = 11, PageNumber = 3, PageSize = 5 });
        }

        private static GetDonorOrganizationContributionsResult Page(IReadOnlyList<ContributionDto> contributions, int totalCount, int pageNumber = 1, int pageSize = 20) => new()
        {
            Contributions = contributions,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        [TestMethod]
        public async Task GetDonorOrganizationContributions_Administrator_PassesTheAdministratorFlag()
        {
            _mediator.Setup(m => m.Send(
                    It.Is<GetDonorOrganizationContributionsQuery>(q => q.IsAdministrator),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<GetDonorOrganizationContributionsResult>.Success(Page([], totalCount: 0)));

            var result = await CreateSut(admin: true).GetDonorOrganizationContributions(OrganizationId);

            result.Should().BeOfType<OkObjectResult>();
        }

        [TestMethod]
        public async Task GetDonorOrganizationContributions_NotAMember_ReturnsForbidden()
        {
            _mediator.Setup(m => m.Send(It.IsAny<GetDonorOrganizationContributionsQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<GetDonorOrganizationContributionsResult>.Forbidden("Not a member."));

            var result = await CreateSut().GetDonorOrganizationContributions(OrganizationId);

            result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        }

        [TestMethod]
        public async Task GetDonorOrganizationContributions_UnknownOrganization_ReturnsNotFound()
        {
            _mediator.Setup(m => m.Send(It.IsAny<GetDonorOrganizationContributionsQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<GetDonorOrganizationContributionsResult>.NotFound("Not found."));

            var result = await CreateSut().GetDonorOrganizationContributions(OrganizationId);

            result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        }
    }
}