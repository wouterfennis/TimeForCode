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
    public class GetProjectPublicSummaryHandlerTests
    {
        private const string ProjectId = "507f1f77bcf86cd799439031";

        private Mock<IDonationDataSource> _donationData = default!;
        private GetProjectPublicSummaryHandler _sut = default!;

        [TestInitialize]
        public void Setup()
        {
            _donationData = new Mock<IDonationDataSource>();
            _sut = new GetProjectPublicSummaryHandler(_donationData.Object, NullLogger<GetProjectPublicSummaryHandler>.Instance);

            GivenDonations();
            GivenContributions();
        }

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
        public async Task Handle_ProjectWithDonationsAndContributions_ReturnsOnlyTheTotals()
        {
            GivenDonations(new ProjectDonation("d1", "507f1f77bcf86cd799439041", DonationStatus.Active, 10, 4, DateTimeOffset.UnixEpoch));
            GivenContributions(new ProjectContribution("c1", "alice", 4));

            var result = await _sut.Handle(new GetProjectPublicSummaryQuery { ProjectId = ProjectId }, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.HoursReceived.Should().Be(10);
            result.Value.HoursLogged.Should().Be(4);
            result.Value.ProgressPercentage.Should().Be(40);
            result.Value.DonorCount.Should().Be(1);
            result.Value.ContributorCount.Should().Be(1);
        }

        [TestMethod]
        public async Task Handle_ProjectWithoutDonations_ReturnsZeros()
        {
            var result = await _sut.Handle(new GetProjectPublicSummaryQuery { ProjectId = ProjectId }, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.HoursReceived.Should().Be(0);
            result.Value.DonorCount.Should().Be(0);
            result.Value.ContributorCount.Should().Be(0);
        }

        [TestMethod]
        public async Task Handle_ProjectNotFound_ReturnsTheFailure()
        {
            _donationData
                .Setup(d => d.GetProjectDonationsAsync(ProjectId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<IReadOnlyList<ProjectDonation>>.Failure("Project not found.", HttpStatusCode.NotFound));

            var result = await _sut.Handle(new GetProjectPublicSummaryQuery { ProjectId = ProjectId }, CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            result.FailureStatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task Handle_ContributionsCannotBeRead_ReturnsTheFailure()
        {
            _donationData
                .Setup(d => d.GetProjectContributionsAsync(ProjectId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<IReadOnlyList<ProjectContribution>>.Failure("Unreachable.", HttpStatusCode.BadGateway));

            var result = await _sut.Handle(new GetProjectPublicSummaryQuery { ProjectId = ProjectId }, CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            result.FailureStatusCode.Should().Be(HttpStatusCode.BadGateway);
        }
    }
}