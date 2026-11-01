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
    public class GetContributionHistoryExportHandlerTests
    {
        private const string OrganizationId = "507f1f77bcf86cd799439013";
        private const string AccessToken = "token-of-the-caller";

        private Mock<IDonationDataSource> _donationData = default!;
        private GetContributionHistoryExportHandler _sut = default!;

        [TestInitialize]
        public void Setup()
        {
            _donationData = new Mock<IDonationDataSource>();
            _sut = new GetContributionHistoryExportHandler(_donationData.Object, NullLogger<GetContributionHistoryExportHandler>.Instance);
        }

        private static GetContributionHistoryExportQuery Query(ExportFormat format) =>
            new() { DonorOrganizationId = OrganizationId, AccessToken = AccessToken, Format = format };

        private void GivenContributions(params OrganizationContribution[] contributions)
        {
            _donationData
                .Setup(d => d.GetContributionsAsync(OrganizationId, AccessToken, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<IReadOnlyList<OrganizationContribution>>.Success(contributions));
        }

        private static OrganizationContribution Contribution(int hours) =>
            new("507f1f77bcf86cd799439031", "507f1f77bcf86cd799439021", "octocat", hours, null, new DateTimeOffset(2026, 10, 2, 9, 0, 0, TimeSpan.Zero));

        [TestMethod]
        public async Task Handle_Csv_ReturnsACsvFileNamedAfterTheOrganization()
        {
            GivenContributions(Contribution(4));

            var result = await _sut.Handle(Query(ExportFormat.Csv), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.ContentType.Should().Be("text/csv");
            result.Value.FileName.Should().Be($"contribution-history-{OrganizationId}.csv");
            result.Value.Content.Should().Contain("octocat,4,");
        }

        [TestMethod]
        public async Task Handle_Json_ReturnsAJsonFileNamedAfterTheOrganization()
        {
            GivenContributions(Contribution(4));

            var result = await _sut.Handle(Query(ExportFormat.Json), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.ContentType.Should().Be("application/json");
            result.Value.FileName.Should().Be($"contribution-history-{OrganizationId}.json");
            result.Value.Content.Should().Contain("\"hours\":4");
        }

        [TestMethod]
        public async Task Handle_OrganizationWithoutContributions_ReturnsAnExportWithoutRows()
        {
            GivenContributions();

            var result = await _sut.Handle(Query(ExportFormat.Csv), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Content.Should().Be("ContributionId,ProjectId,ContributorLogin,Hours,GithubReference,LoggedAt\r\n");
        }

        [TestMethod]
        [DataRow(HttpStatusCode.Forbidden)]
        [DataRow(HttpStatusCode.NotFound)]
        [DataRow(HttpStatusCode.Unauthorized)]
        public async Task Handle_DataSourceRefuses_ReturnsTheSameFailure(HttpStatusCode status)
        {
            _donationData
                .Setup(d => d.GetContributionsAsync(OrganizationId, AccessToken, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<IReadOnlyList<OrganizationContribution>>.Failure("Refused.", status));

            var result = await _sut.Handle(Query(ExportFormat.Json), CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            result.FailureStatusCode.Should().Be(status);
        }
    }
}