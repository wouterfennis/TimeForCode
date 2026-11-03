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
    public class GetProjectDonationReportHandlerTests
    {
        private const string ProjectId = "507f1f77bcf86cd799439031";
        private const string AccessToken = "token-of-the-caller";

        private Mock<IDonationDataSource> _donationData = default!;
        private GetProjectDonationReportHandler _sut = default!;

        [TestInitialize]
        public void Setup()
        {
            _donationData = new Mock<IDonationDataSource>();
            _sut = new GetProjectDonationReportHandler(_donationData.Object, NullLogger<GetProjectDonationReportHandler>.Instance);

            _donationData
                .Setup(d => d.VerifyMaintainerAsync(ProjectId, AccessToken, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<bool>.Success(true));
            GivenDonations();
            GivenContributions();
        }

        private static GetProjectDonationReportQuery Query(bool isAdministrator = false) =>
            new() { ProjectId = ProjectId, AccessToken = AccessToken, IsAdministrator = isAdministrator };

        private void GivenDonations(params ProjectDonation[] donations)
        {
            _donationData
                .Setup(d => d.GetProjectDonationsAsync(ProjectId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<IReadOnlyList<ProjectDonation>>.Success(donations));
        }

        private void GivenContributions(params ProjectContribution[] contributions)
        {
            _donationData
                .Setup(d => d.GetProjectContributionsAsync(ProjectId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<IReadOnlyList<ProjectContribution>>.Success(contributions));
        }

        [TestMethod]
        public async Task Handle_MaintainerOfProject_ReturnsTheDetailedReport()
        {
            GivenDonations(
                new ProjectDonation("d1", null, DonationStatus.Active, 10, 4, DateTimeOffset.UnixEpoch),
                new ProjectDonation("d2", null, DonationStatus.Pending, 5, 0, DateTimeOffset.UnixEpoch));
            GivenContributions(new ProjectContribution("c1", "alice", 4));

            var result = await _sut.Handle(Query(), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.HoursReceived.Should().Be(10);
            result.Value.HoursLogged.Should().Be(4);
            result.Value.ProgressPercentage.Should().Be(40);
            result.Value.ActiveDonations.Should().Be(1);
            result.Value.PendingDonations.Should().Be(1);
            result.Value.Donations.Should().HaveCount(2);
            result.Value.Donations[0].Id.Should().Be("d1");
            result.Value.Contributors.Should().ContainSingle().Which.ContributorLogin.Should().Be("alice");
        }

        [TestMethod]
        public async Task Handle_Administrator_SkipsTheMaintainerCheck()
        {
            var result = await _sut.Handle(Query(isAdministrator: true), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            _donationData.Verify(d => d.VerifyMaintainerAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [TestMethod]
        [DataRow(HttpStatusCode.Forbidden)]
        [DataRow(HttpStatusCode.NotFound)]
        [DataRow(HttpStatusCode.Unauthorized)]
        [DataRow(HttpStatusCode.BadGateway)]
        public async Task Handle_MaintainerCheckFails_ReturnsTheSameFailureWithoutReadingTheData(HttpStatusCode status)
        {
            _donationData
                .Setup(d => d.VerifyMaintainerAsync(ProjectId, AccessToken, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<bool>.Failure("Refused.", status));

            var result = await _sut.Handle(Query(), CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            result.FailureStatusCode.Should().Be(status);
            result.ErrorMessage.Should().Be("Refused.");
            _donationData.Verify(d => d.GetProjectDonationsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [TestMethod]
        public async Task Handle_DonationsCannotBeRead_ReturnsTheFailure()
        {
            _donationData
                .Setup(d => d.GetProjectDonationsAsync(ProjectId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<IReadOnlyList<ProjectDonation>>.Failure("Project not found.", HttpStatusCode.NotFound));

            var result = await _sut.Handle(Query(isAdministrator: true), CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            result.FailureStatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task Handle_ContributionsCannotBeRead_ReturnsTheFailure()
        {
            _donationData
                .Setup(d => d.GetProjectContributionsAsync(ProjectId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<IReadOnlyList<ProjectContribution>>.Failure("Unreachable.", HttpStatusCode.BadGateway));

            var result = await _sut.Handle(Query(), CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            result.FailureStatusCode.Should().Be(HttpStatusCode.BadGateway);
        }
    }
}