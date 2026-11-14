using AwesomeAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Net;
using TimeForCode.Donation.Api.V1.Controllers;
using TimeForCode.Donation.Api.V1.Models;
using TimeForCode.Donation.Commands;

namespace TimeForCode.Donation.Api.Tests
{
    [TestClass]
    public class PublicStatisticsControllerTests
    {
        private const string Id = "507f1f77bcf86cd799439011";

        private readonly Mock<IMediator> _mediator = new();

        private PublicStatisticsController CreateSut() => new(_mediator.Object);

        [TestMethod]
        public async Task GetContributorHours_ReturnsTheTotalHours()
        {
            _mediator.Setup(m => m.Send(It.Is<GetContributorHoursQuery>(q => q.ContributorId == Id), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<ContributorHoursResult>.Success(new ContributorHoursResult { TotalHours = 12 }));

            var result = await CreateSut().GetContributorHours(Id);

            var response = result.Should().BeOfType<OkObjectResult>().Subject.Value.Should().BeOfType<ContributorHoursResponse>().Subject;
            response.TotalHours.Should().Be(12);
        }

        [TestMethod]
        public async Task GetContributorHours_UnknownContributor_ReturnsNotFound()
        {
            _mediator.Setup(m => m.Send(It.IsAny<GetContributorHoursQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<ContributorHoursResult>.NotFound("Contributor not found."));

            var result = await CreateSut().GetContributorHours(Id);

            result.Should().BeOfType<ObjectResult>().Subject.StatusCode.Should().Be((int)HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task GetDonorOrganizationHours_ReturnsTheLoggedHours()
        {
            _mediator.Setup(m => m.Send(It.Is<GetDonorOrganizationHoursQuery>(q => q.DonorOrganizationId == Id), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<DonorOrganizationHoursResult>.Success(new DonorOrganizationHoursResult { HoursLogged = 30 }));

            var result = await CreateSut().GetDonorOrganizationHours(Id);

            var response = result.Should().BeOfType<OkObjectResult>().Subject.Value.Should().BeOfType<DonorOrganizationHoursResponse>().Subject;
            response.HoursLogged.Should().Be(30);
        }

        [TestMethod]
        public async Task GetDonorOrganizationHours_UnknownOrganization_ReturnsNotFound()
        {
            _mediator.Setup(m => m.Send(It.IsAny<GetDonorOrganizationHoursQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<DonorOrganizationHoursResult>.NotFound("Donor organization not found."));

            var result = await CreateSut().GetDonorOrganizationHours(Id);

            result.Should().BeOfType<ObjectResult>().Subject.StatusCode.Should().Be((int)HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task GetPlatformStatistics_ReturnsTheTotals()
        {
            _mediator.Setup(m => m.Send(It.IsAny<GetPlatformStatisticsQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<PlatformStatisticsResult>.Success(new PlatformStatisticsResult { TotalHoursLogged = 20, OrganizationsSupported = 2 }));

            var result = await CreateSut().GetPlatformStatistics();

            var response = result.Should().BeOfType<OkObjectResult>().Subject.Value.Should().BeOfType<PlatformStatisticsResponse>().Subject;
            response.TotalHoursLogged.Should().Be(20);
            response.OrganizationsSupported.Should().Be(2);
        }
    }
}