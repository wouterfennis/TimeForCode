using AwesomeAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Net;
using System.Text;
using TimeForCode.Reporting.Api.V1.Controllers;
using TimeForCode.Reporting.Api.V1.Models;
using TimeForCode.Reporting.Commands;
using TimeForCode.Reporting.Values;

namespace TimeForCode.Reporting.Api.Tests
{
    [TestClass]
    public class ReportingControllerTests
    {
        private const string OrganizationId = "507f1f77bcf86cd799439011";
        private const string AccessToken = "token-of-the-caller";

        private readonly Mock<IMediator> _mediator = new();

        private ReportingController CreateSut(string? authorization = "Bearer " + AccessToken)
        {
            var httpContext = new DefaultHttpContext();
            if (authorization != null)
            {
                httpContext.Request.Headers.Authorization = authorization;
            }

            return new ReportingController(_mediator.Object)
            {
                ControllerContext = new ControllerContext { HttpContext = httpContext }
            };
        }

        private static ImpactReportResult Report() => new()
        {
            HoursPledged = 10,
            HoursLogged = 4,
            ProjectsSupported = 1,
            Projects = [new ProjectImpactDto { ProjectId = "507f1f77bcf86cd799439021", HoursPledged = 10, HoursLogged = 4 }]
        };

        private static ContributionHistoryExportResult Export(string content, string contentType, string fileName) => new()
        {
            Content = content,
            ContentType = contentType,
            FileName = fileName
        };

        [TestMethod]
        public async Task GetImpactReport_NoBearerToken_ReturnsBadRequest()
        {
            var result = await CreateSut(authorization: null).GetImpactReport(OrganizationId);

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [TestMethod]
        public async Task GetImpactReport_NotABearerToken_ReturnsBadRequest()
        {
            var result = await CreateSut(authorization: "Basic abc").GetImpactReport(OrganizationId);

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [TestMethod]
        public async Task GetImpactReport_Allowed_ReturnsTheReportAndPassesTheCallersToken()
        {
            _mediator.Setup(m => m.Send(
                    It.Is<GetImpactReportQuery>(q => q.DonorOrganizationId == OrganizationId && q.AccessToken == AccessToken),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<ImpactReportResult>.Success(Report()));

            var result = await CreateSut().GetImpactReport(OrganizationId);

            var response = result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeOfType<ImpactReportResponse>().Which;
            response.HoursPledged.Should().Be(10);
            response.HoursLogged.Should().Be(4);
            response.ProjectsSupported.Should().Be(1);
            response.Projects.Should().ContainSingle().Which.ProjectId.Should().Be("507f1f77bcf86cd799439021");
        }

        [TestMethod]
        [DataRow(HttpStatusCode.Unauthorized, StatusCodes.Status401Unauthorized)]
        [DataRow(HttpStatusCode.Forbidden, StatusCodes.Status403Forbidden)]
        [DataRow(HttpStatusCode.NotFound, StatusCodes.Status404NotFound)]
        [DataRow(HttpStatusCode.BadGateway, StatusCodes.Status502BadGateway)]
        [DataRow(HttpStatusCode.InternalServerError, StatusCodes.Status400BadRequest)]
        public async Task GetImpactReport_Refused_ReturnsTheMatchingProblem(HttpStatusCode refusal, int expected)
        {
            _mediator.Setup(m => m.Send(It.IsAny<GetImpactReportQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<ImpactReportResult>.Failure("Refused.", refusal));

            var result = await CreateSut().GetImpactReport(OrganizationId);

            var problem = result.Should().BeOfType<ObjectResult>().Which;
            problem.StatusCode.Should().Be(expected);
            problem.Value.Should().BeOfType<ProblemDetails>().Which.Detail.Should().Be("Refused.");
        }

        [TestMethod]
        public async Task GetContributionHistory_NoBearerToken_ReturnsBadRequest()
        {
            var result = await CreateSut(authorization: null).GetContributionHistory(OrganizationId, ExportFormat.Csv);

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [TestMethod]
        public async Task GetContributionHistory_Csv_ReturnsTheFileAndAsksForTheRequestedFormat()
        {
            _mediator.Setup(m => m.Send(
                    It.Is<GetContributionHistoryExportQuery>(q =>
                        q.DonorOrganizationId == OrganizationId && q.AccessToken == AccessToken && q.Format == ExportFormat.Csv),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<ContributionHistoryExportResult>.Success(Export("a,b\r\n", "text/csv", "history.csv")));

            var result = await CreateSut().GetContributionHistory(OrganizationId, ExportFormat.Csv);

            var file = result.Should().BeOfType<FileContentResult>().Which;
            file.ContentType.Should().Be("text/csv");
            file.FileDownloadName.Should().Be("history.csv");
            Encoding.UTF8.GetString(file.FileContents).Should().Be("a,b\r\n");
        }

        [TestMethod]
        public async Task GetContributionHistory_Json_ReturnsTheFile()
        {
            _mediator.Setup(m => m.Send(
                    It.Is<GetContributionHistoryExportQuery>(q => q.Format == ExportFormat.Json),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<ContributionHistoryExportResult>.Success(Export("{\"contributions\":[]}", "application/json", "history.json")));

            var result = await CreateSut().GetContributionHistory(OrganizationId, ExportFormat.Json);

            result.Should().BeOfType<FileContentResult>().Which.ContentType.Should().Be("application/json");
        }

        [TestMethod]
        [DataRow(HttpStatusCode.Unauthorized, StatusCodes.Status401Unauthorized)]
        [DataRow(HttpStatusCode.Forbidden, StatusCodes.Status403Forbidden)]
        [DataRow(HttpStatusCode.NotFound, StatusCodes.Status404NotFound)]
        [DataRow(HttpStatusCode.BadGateway, StatusCodes.Status502BadGateway)]
        public async Task GetContributionHistory_Refused_ReturnsTheMatchingProblem(HttpStatusCode refusal, int expected)
        {
            _mediator.Setup(m => m.Send(It.IsAny<GetContributionHistoryExportQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<ContributionHistoryExportResult>.Failure("Refused.", refusal));

            var result = await CreateSut().GetContributionHistory(OrganizationId, ExportFormat.Csv);

            result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(expected);
        }
    }
}