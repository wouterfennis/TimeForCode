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
    public class GetImpactReportHandlerTests
    {
        private const string OrganizationId = "507f1f77bcf86cd799439013";
        private const string AccessToken = "token-of-the-caller";

        private Mock<IDonationDataSource> _donationData = default!;
        private GetImpactReportHandler _sut = default!;

        [TestInitialize]
        public void Setup()
        {
            _donationData = new Mock<IDonationDataSource>();
            _sut = new GetImpactReportHandler(_donationData.Object, NullLogger<GetImpactReportHandler>.Instance);
        }

        private static GetImpactReportQuery Query() => new() { DonorOrganizationId = OrganizationId, AccessToken = AccessToken };

        private void GivenDonations(params OrganizationDonation[] donations)
        {
            _donationData
                .Setup(d => d.GetDonationsAsync(OrganizationId, AccessToken, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<IReadOnlyList<OrganizationDonation>>.Success(donations));
        }

        [TestMethod]
        public async Task Handle_OrganizationWithDonations_ReturnsTheImpactReport()
        {
            GivenDonations(
                new OrganizationDonation("507f1f77bcf86cd799439021", DonationStatus.Active, 10, 4),
                new OrganizationDonation("507f1f77bcf86cd799439022", DonationStatus.Completed, 6, 6));

            var result = await _sut.Handle(Query(), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.HoursPledged.Should().Be(16);
            result.Value.HoursLogged.Should().Be(10);
            result.Value.ProjectsSupported.Should().Be(2);
            result.Value.Projects.Should().HaveCount(2);
            result.Value.Projects[0].ProjectId.Should().Be("507f1f77bcf86cd799439021");
            result.Value.Projects[0].HoursPledged.Should().Be(10);
            result.Value.Projects[0].HoursLogged.Should().Be(4);
        }

        [TestMethod]
        public async Task Handle_OrganizationWithoutDonations_ReturnsAnEmptyReport()
        {
            GivenDonations();

            var result = await _sut.Handle(Query(), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.HoursPledged.Should().Be(0);
            result.Value.ProjectsSupported.Should().Be(0);
            result.Value.Projects.Should().BeEmpty();
        }

        [TestMethod]
        [DataRow(HttpStatusCode.Forbidden)]
        [DataRow(HttpStatusCode.NotFound)]
        [DataRow(HttpStatusCode.Unauthorized)]
        public async Task Handle_DataSourceRefuses_ReturnsTheSameFailure(HttpStatusCode status)
        {
            _donationData
                .Setup(d => d.GetDonationsAsync(OrganizationId, AccessToken, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<IReadOnlyList<OrganizationDonation>>.Failure("Refused.", status));

            var result = await _sut.Handle(Query(), CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            result.FailureStatusCode.Should().Be(status);
            result.ErrorMessage.Should().Be("Refused.");
        }
    }
}