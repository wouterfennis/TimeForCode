using AwesomeAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Moq;
using System.Net;
using System.Security.Claims;
using TimeForCode.Reporting.Api.V1.Controllers;
using TimeForCode.Reporting.Api.V1.Models;
using TimeForCode.Reporting.Commands;
using TimeForCode.Reporting.Values;

namespace TimeForCode.Reporting.Api.Tests
{
    [TestClass]
    public class ProjectReportingControllerTests
    {
        private const string ProjectId = "507f1f77bcf86cd799439031";
        private const string AccessToken = "token-of-the-caller";

        private readonly Mock<IMediator> _mediator = new();

        private ProjectReportingController CreateSut(string? authorization = "Bearer " + AccessToken, string scope = "user")
        {
            var httpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("scope", scope)], "test"))
            };
            if (authorization != null)
            {
                httpContext.Request.Headers.Authorization = authorization;
            }

            return new ProjectReportingController(_mediator.Object)
            {
                ControllerContext = new ControllerContext { HttpContext = httpContext }
            };
        }

        private static ProjectDonationReportResult Report() => new()
        {
            HoursReceived = 10,
            HoursLogged = 4,
            ProgressPercentage = 40,
            DonorCount = 1,
            ContributorCount = 1,
            PendingDonations = 0,
            ActiveDonations = 1,
            CompletedDonations = 0,
            CancelledDonations = 0,
            Donations =
            [
                new ProjectDonationDto
                {
                    Id = "507f1f77bcf86cd799439041",
                    DonorOrganizationId = "507f1f77bcf86cd799439013",
                    Status = DonationStatus.Active,
                    HoursCommitted = 10,
                    HoursLogged = 4,
                    CreatedAt = new DateTimeOffset(2026, 10, 1, 9, 0, 0, TimeSpan.Zero)
                }
            ],
            Contributors = [new ProjectContributorDto { ContributorId = "507f1f77bcf86cd799439061", ContributorLogin = "alice", Hours = 4 }]
        };

        [TestMethod]
        public async Task GetDonationReport_NoBearerToken_ReturnsBadRequest()
        {
            var result = await CreateSut(authorization: null).GetDonationReport(ProjectId);

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [TestMethod]
        public async Task GetDonationReport_Maintainer_ReturnsTheReportAndPassesTheCallersToken()
        {
            _mediator.Setup(m => m.Send(
                    It.Is<GetProjectDonationReportQuery>(q => q.ProjectId == ProjectId && q.AccessToken == AccessToken && !q.IsAdministrator),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<ProjectDonationReportResult>.Success(Report()));

            var result = await CreateSut().GetDonationReport(ProjectId);

            var response = result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeOfType<ProjectDonationReportResponse>().Which;
            response.HoursReceived.Should().Be(10);
            response.HoursLogged.Should().Be(4);
            response.ProgressPercentage.Should().Be(40);
            response.DonorCount.Should().Be(1);
            response.ContributorCount.Should().Be(1);
            response.ActiveDonations.Should().Be(1);
            response.Donations.Should().ContainSingle().Which.Status.Should().Be(DonationStatus.Active);
            response.Donations[0].HoursCommitted.Should().Be(10);
            response.Contributors.Should().ContainSingle().Which.ContributorLogin.Should().Be("alice");
        }

        [TestMethod]
        public async Task GetDonationReport_Administrator_AsksWithTheAdministratorFlag()
        {
            _mediator.Setup(m => m.Send(
                    It.Is<GetProjectDonationReportQuery>(q => q.IsAdministrator),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<ProjectDonationReportResult>.Success(Report()));

            var result = await CreateSut(scope: "admin").GetDonationReport(ProjectId);

            result.Should().BeOfType<OkObjectResult>();
        }

        [TestMethod]
        [DataRow(HttpStatusCode.Unauthorized, StatusCodes.Status401Unauthorized)]
        [DataRow(HttpStatusCode.Forbidden, StatusCodes.Status403Forbidden)]
        [DataRow(HttpStatusCode.NotFound, StatusCodes.Status404NotFound)]
        [DataRow(HttpStatusCode.BadGateway, StatusCodes.Status502BadGateway)]
        [DataRow(HttpStatusCode.InternalServerError, StatusCodes.Status400BadRequest)]
        public async Task GetDonationReport_Refused_ReturnsTheMatchingProblem(HttpStatusCode refusal, int expected)
        {
            _mediator.Setup(m => m.Send(It.IsAny<GetProjectDonationReportQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<ProjectDonationReportResult>.Failure("Refused.", refusal));

            var result = await CreateSut().GetDonationReport(ProjectId);

            var problem = result.Should().BeOfType<ObjectResult>().Which;
            problem.StatusCode.Should().Be(expected);
            problem.Value.Should().BeOfType<ProblemDetails>().Which.Detail.Should().Be("Refused.");
        }

        [TestMethod]
        public async Task GetPublicSummary_KnownProject_ReturnsTheTotalsWithoutAToken()
        {
            _mediator.Setup(m => m.Send(
                    It.Is<GetProjectPublicSummaryQuery>(q => q.ProjectId == ProjectId),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<ProjectPublicSummaryResult>.Success(new ProjectPublicSummaryResult
                {
                    HoursReceived = 10,
                    HoursLogged = 4,
                    ProgressPercentage = 40,
                    DonorCount = 1,
                    ContributorCount = 2
                }));

            var result = await CreateSut(authorization: null).GetPublicSummary(ProjectId);

            var response = result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeOfType<ProjectPublicSummaryResponse>().Which;
            response.HoursReceived.Should().Be(10);
            response.HoursLogged.Should().Be(4);
            response.ProgressPercentage.Should().Be(40);
            response.DonorCount.Should().Be(1);
            response.ContributorCount.Should().Be(2);
        }

        [TestMethod]
        public void GetPublicSummary_Anonymous_UsesTheStricterPublicRateLimit()
        {
            var attribute = typeof(ProjectReportingController)
                .GetMethod(nameof(ProjectReportingController.GetPublicSummary))!
                .GetCustomAttributes(typeof(EnableRateLimitingAttribute), inherit: false)
                .Cast<EnableRateLimitingAttribute>()
                .Single();

            attribute.PolicyName.Should().Be("public");
        }

        [TestMethod]
        [DataRow(HttpStatusCode.NotFound, StatusCodes.Status404NotFound)]
        [DataRow(HttpStatusCode.BadGateway, StatusCodes.Status502BadGateway)]
        public async Task GetPublicSummary_Failed_ReturnsTheMatchingProblem(HttpStatusCode failure, int expected)
        {
            _mediator.Setup(m => m.Send(It.IsAny<GetProjectPublicSummaryQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<ProjectPublicSummaryResult>.Failure("Failed.", failure));

            var result = await CreateSut(authorization: null).GetPublicSummary(ProjectId);

            result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(expected);
        }
    }
}