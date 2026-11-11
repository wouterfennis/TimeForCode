using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using Moq;
using System.Net;
using TimeForCode.Donation.Application.Handlers;
using TimeForCode.Donation.Application.Interfaces;
using TimeForCode.Donation.Commands;
using TimeForCode.Donation.Domain;
using TimeForCode.Donation.Values;

namespace TimeForCode.Donation.Application.Tests.Handlers
{
    [TestClass]
    public class GetPublicHoursHandlersTests
    {
        private static readonly ObjectId ContributorId = new("5f43a0e74b12c84f1b0000c1");
        private static readonly ObjectId OrganizationId = new("5f43a0e74b12c84f1b0000d1");

        [TestMethod]
        public async Task ContributorHours_ReturnsTheSumOfTheLoggedHours()
        {
            var contributors = new Mock<IContributorRepository>();
            var contributions = new Mock<IContributionRepository>();
            contributors.Setup(r => r.GetByIdAsync(ContributorId)).ReturnsAsync(Contributor.Create("user-1", "octocat", "https://avatars.githubusercontent.com/u/1"));
            contributions.Setup(r => r.GetTotalHoursByContributorIdAsync(ContributorId, It.IsAny<CancellationToken>())).ReturnsAsync(25);
            var sut = new GetContributorHoursHandler(contributors.Object, contributions.Object, NullLogger<GetContributorHoursHandler>.Instance);

            var result = await sut.Handle(new GetContributorHoursQuery { ContributorId = ContributorId.ToString() }, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.TotalHours.Should().Be(25);
        }

        [TestMethod]
        public async Task ContributorHours_UnknownContributor_IsNotFound()
        {
            var contributors = new Mock<IContributorRepository>();
            var contributions = new Mock<IContributionRepository>();
            contributors.Setup(r => r.GetByIdAsync(ContributorId)).ReturnsAsync((Contributor?)null);
            var sut = new GetContributorHoursHandler(contributors.Object, contributions.Object, NullLogger<GetContributorHoursHandler>.Instance);

            var result = await sut.Handle(new GetContributorHoursQuery { ContributorId = ContributorId.ToString() }, CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            result.FailureStatusCode.Should().Be(HttpStatusCode.NotFound);
            contributions.Verify(r => r.GetTotalHoursByContributorIdAsync(It.IsAny<ObjectId>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [TestMethod]
        public async Task OrganizationHours_CountsOnlyActiveAndCompletedDonations()
        {
            var organizations = new Mock<IDonorOrganizationRepository>();
            var donations = new Mock<IDonationRepository>();
            organizations.Setup(r => r.GetByIdAsync(OrganizationId.ToString())).ReturnsAsync(DonorOrganization.Create("Acme", null, null));
            donations.Setup(r => r.GetByDonorOrganizationIdsAsync(It.Is<IReadOnlyCollection<string>>(ids => ids.Single() == OrganizationId.ToString()), It.IsAny<CancellationToken>()))
                .ReturnsAsync([
                    DonationTestData.Build(DonationStatus.Active, committed: 20, logged: 7, organizationId: OrganizationId),
                    DonationTestData.Build(DonationStatus.Completed, committed: 10, organizationId: OrganizationId),
                    DonationTestData.Build(DonationStatus.Pending, committed: 10, organizationId: OrganizationId),
                    DonationTestData.Build(DonationStatus.Cancelled, committed: 10, logged: 3, organizationId: OrganizationId)]);
            var sut = new GetDonorOrganizationHoursHandler(organizations.Object, donations.Object, NullLogger<GetDonorOrganizationHoursHandler>.Instance);

            var result = await sut.Handle(new GetDonorOrganizationHoursQuery { DonorOrganizationId = OrganizationId.ToString() }, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.HoursLogged.Should().Be(17);
        }

        [TestMethod]
        public async Task OrganizationHours_UnknownOrganization_IsNotFound()
        {
            var organizations = new Mock<IDonorOrganizationRepository>();
            var donations = new Mock<IDonationRepository>();
            organizations.Setup(r => r.GetByIdAsync(OrganizationId.ToString())).ReturnsAsync((DonorOrganization?)null);
            var sut = new GetDonorOrganizationHoursHandler(organizations.Object, donations.Object, NullLogger<GetDonorOrganizationHoursHandler>.Instance);

            var result = await sut.Handle(new GetDonorOrganizationHoursQuery { DonorOrganizationId = OrganizationId.ToString() }, CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            result.FailureStatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task PlatformStatistics_ReturnsTheTotalsOfTheRepository()
        {
            var donations = new Mock<IDonationRepository>();
            donations.Setup(r => r.GetReceivedTotalsAsync(It.IsAny<CancellationToken>())).ReturnsAsync((20, 2));
            var sut = new GetPlatformStatisticsHandler(donations.Object);

            var result = await sut.Handle(new GetPlatformStatisticsQuery(), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.TotalHoursLogged.Should().Be(20);
            result.Value.OrganizationsSupported.Should().Be(2);
        }
    }
}