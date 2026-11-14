using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RichardSzalay.MockHttp;
using System.Net;
using TimeForCode.Reporting.Domain;
using TimeForCode.Reporting.Infrastructure.Services;

namespace TimeForCode.Reporting.Infrastructure.Tests
{
    [TestClass]
    public class DonationApiBadgeDataSourceTests
    {
        private const string BaseUri = "http://donation.test/";
        private const string Id = "507f1f77bcf86cd799439013";

        private MockHttpMessageHandler _mockHttp = default!;
        private DonationApiDataSource _sut = default!;

        [TestInitialize]
        public void Setup()
        {
            _mockHttp = new MockHttpMessageHandler();

            var factory = new Mock<IHttpClientFactory>();
            factory.Setup(f => f.CreateClient(DonationApiDataSource.HttpClientName)).Returns(() =>
            {
                HttpClient client = _mockHttp.ToHttpClient();
                client.BaseAddress = new Uri(BaseUri);
                return client;
            });

            _sut = new DonationApiDataSource(factory.Object, NullLogger<DonationApiDataSource>.Instance);
        }

        [TestMethod]
        public async Task GetOrganizationHoursAsync_ReturnsTheHoursAndSendsNoToken()
        {
            _mockHttp.Expect(HttpMethod.Get, BaseUri + "api/v1/donororganization/" + Id + "/hours")
                .With(request => request.Headers.Authorization is null)
                .Respond("application/json", """{"hoursLogged":17}""");

            var result = await _sut.GetOrganizationHoursAsync(Id, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().Be(17);
            _mockHttp.VerifyNoOutstandingExpectation();
        }

        [TestMethod]
        public async Task GetOrganizationHoursAsync_UnknownOrganization_IsNotFound()
        {
            _mockHttp.When(HttpMethod.Get, BaseUri + "api/v1/donororganization/" + Id + "/hours").Respond(HttpStatusCode.NotFound);

            var result = await _sut.GetOrganizationHoursAsync(Id, CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            result.FailureStatusCode.Should().Be(HttpStatusCode.NotFound);
            result.ErrorMessage.Should().Be("Donor organization not found.");
        }

        [TestMethod]
        public async Task GetContributorHoursAsync_ReturnsTheTotalHours()
        {
            _mockHttp.Expect(HttpMethod.Get, BaseUri + "api/v1/contributor/" + Id + "/hours")
                .Respond("application/json", """{"totalHours":25}""");

            var result = await _sut.GetContributorHoursAsync(Id, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().Be(25);
        }

        [TestMethod]
        public async Task GetContributorHoursAsync_UnknownContributor_IsNotFound()
        {
            _mockHttp.When(HttpMethod.Get, BaseUri + "api/v1/contributor/" + Id + "/hours").Respond(HttpStatusCode.NotFound);

            var result = await _sut.GetContributorHoursAsync(Id, CancellationToken.None);

            result.FailureStatusCode.Should().Be(HttpStatusCode.NotFound);
            result.ErrorMessage.Should().Be("Contributor not found.");
        }

        [TestMethod]
        public async Task GetPlatformStatisticsAsync_ReturnsTheTotals()
        {
            _mockHttp.Expect(HttpMethod.Get, BaseUri + "api/v1/statistics")
                .Respond("application/json", """{"totalHoursLogged":20,"organizationsSupported":2}""");

            var result = await _sut.GetPlatformStatisticsAsync(CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().Be(new PlatformStatistics(20, 2));
        }

        [TestMethod]
        public async Task GetPlatformStatisticsAsync_DonationApiFails_IsABadGateway()
        {
            _mockHttp.When(HttpMethod.Get, BaseUri + "api/v1/statistics").Respond(HttpStatusCode.InternalServerError);

            var result = await _sut.GetPlatformStatisticsAsync(CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            result.FailureStatusCode.Should().Be(HttpStatusCode.BadGateway);
        }
    }
}