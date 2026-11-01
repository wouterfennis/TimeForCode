using AwesomeAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using TimeForCode.Donation.Api.V1.Controllers;
using TimeForCode.Donation.Api.V1.Models;
using TimeForCode.Donation.Commands;
using TimeForCode.Donation.Values;

namespace TimeForCode.Donation.Api.Tests
{
    [TestClass]
    public class DonationControllerTests
    {
        private const string UserId = "user-1";
        private const string OrganizationId = "507f1f77bcf86cd799439011";

        private readonly Mock<IMediator> _mediator = new();

        private DonationController CreateSut(bool withIdentity = true, bool admin = false)
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

            return new DonationController(_mediator.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test")) }
                }
            };
        }

        private static DonationDto Donation() => new()
        {
            Id = "507f1f77bcf86cd799439012",
            ProjectId = "507f1f77bcf86cd799439013",
            DonorOrganizationId = OrganizationId,
            Status = DonationStatus.Active,
            HoursCommitted = 10,
            HoursLogged = 4,
            ProgressPercentage = 40,
            CreatedAt = new DateTimeOffset(2026, 10, 1, 9, 0, 0, TimeSpan.Zero)
        };

        [TestMethod]
        public async Task GetDonorOrganizationDonations_NoIdentity_ReturnsBadRequest()
        {
            var result = await CreateSut(withIdentity: false).GetDonorOrganizationDonations(OrganizationId);

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [TestMethod]
        public async Task GetDonorOrganizationDonations_Allowed_ReturnsTheDonations()
        {
            _mediator.Setup(m => m.Send(
                    It.Is<GetDonorOrganizationDonationsQuery>(q => q.DonorOrganizationId == OrganizationId && q.CallerUserId == UserId && !q.IsAdministrator),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<GetDonorOrganizationDonationsResult>.Success(Page([Donation()], totalCount: 1)));

            var result = await CreateSut().GetDonorOrganizationDonations(OrganizationId);

            result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeOfType<GetDonorOrganizationDonationsResponse>()
                .Which.Donations.Should().ContainSingle().Which.HoursLogged.Should().Be(4);
        }

        [TestMethod]
        public async Task GetDonorOrganizationDonations_PageRequested_PassesThePageAndReturnsThePagingFields()
        {
            _mediator.Setup(m => m.Send(
                    It.Is<GetDonorOrganizationDonationsQuery>(q => q.PageNumber == 3 && q.PageSize == 5),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<GetDonorOrganizationDonationsResult>.Success(Page([Donation()], totalCount: 11, pageNumber: 3, pageSize: 5)));

            var result = await CreateSut().GetDonorOrganizationDonations(OrganizationId, pageNumber: 3, pageSize: 5);

            result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeEquivalentTo(new { TotalCount = 11, PageNumber = 3, PageSize = 5 });
        }

        private static GetDonorOrganizationDonationsResult Page(IReadOnlyList<DonationDto> donations, int totalCount, int pageNumber = 1, int pageSize = 20) => new()
        {
            Donations = donations,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        [TestMethod]
        public async Task GetDonorOrganizationDonations_Administrator_PassesTheAdministratorFlag()
        {
            _mediator.Setup(m => m.Send(
                    It.Is<GetDonorOrganizationDonationsQuery>(q => q.IsAdministrator),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<GetDonorOrganizationDonationsResult>.Success(Page([], totalCount: 0)));

            var result = await CreateSut(admin: true).GetDonorOrganizationDonations(OrganizationId);

            result.Should().BeOfType<OkObjectResult>();
        }

        [TestMethod]
        public async Task GetDonorOrganizationDonations_NotAMember_ReturnsForbidden()
        {
            _mediator.Setup(m => m.Send(It.IsAny<GetDonorOrganizationDonationsQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<GetDonorOrganizationDonationsResult>.Forbidden("Not a member."));

            var result = await CreateSut().GetDonorOrganizationDonations(OrganizationId);

            result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        }

        [TestMethod]
        public async Task GetDonorOrganizationDonations_UnknownOrganization_ReturnsNotFound()
        {
            _mediator.Setup(m => m.Send(It.IsAny<GetDonorOrganizationDonationsQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<GetDonorOrganizationDonationsResult>.NotFound("Not found."));

            var result = await CreateSut().GetDonorOrganizationDonations(OrganizationId);

            result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        }
    }
}