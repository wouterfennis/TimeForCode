using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RichardSzalay.MockHttp;
using System.Net;
using TimeForCode.Reporting.Domain;
using TimeForCode.Reporting.Infrastructure.Services;
using TimeForCode.Reporting.Values;

namespace TimeForCode.Reporting.Infrastructure.Tests
{
    [TestClass]
    public class DonationApiDataSourceTests
    {
        private const string BaseUri = "http://donation.test/";
        private const string OrganizationId = "507f1f77bcf86cd799439013";
        private const string AccessToken = "token-of-the-caller";
        private const string DonationsUrl = BaseUri + "api/v1/donororganization/" + OrganizationId + "/donation";
        private const string ContributionsUrl = BaseUri + "api/v1/donororganization/" + OrganizationId + "/contribution";

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

        private const string DonationsJson = """
            {"donations":[
              {"id":"507f1f77bcf86cd799439041","projectId":"507f1f77bcf86cd799439021","donorOrganizationId":"507f1f77bcf86cd799439013","status":"Active","hoursCommitted":10,"hoursLogged":4,"progressPercentage":40,"createdAt":"2026-10-01T09:00:00+00:00"},
              {"id":"507f1f77bcf86cd799439042","projectId":"507f1f77bcf86cd799439022","status":"Completed","hoursCommitted":6,"hoursLogged":6,"progressPercentage":100,"createdAt":"2026-10-02T09:00:00+00:00"}
            ]}
            """;

        private const string ContributionsJson = """
            {"contributions":[
              {"id":"507f1f77bcf86cd799439031","projectId":"507f1f77bcf86cd799439021","contributorId":"507f1f77bcf86cd799439051","contributorGitHubLogin":"octocat","contributorAvatarUrl":"https://avatars.githubusercontent.com/u/1","hours":4,"githubReference":"https://github.com/acme/app/pull/7","loggedAt":"2026-10-02T09:00:00+00:00"},
              {"id":"507f1f77bcf86cd799439032","projectId":"507f1f77bcf86cd799439021","contributorId":"507f1f77bcf86cd799439051","contributorGitHubLogin":"octocat","contributorAvatarUrl":"https://avatars.githubusercontent.com/u/1","hours":2,"loggedAt":"2026-10-03T09:00:00+00:00"}
            ]}
            """;

        [TestMethod]
        public async Task GetDonationsAsync_Allowed_ReturnsTheDonationsAndSendsTheCallersToken()
        {
            _mockHttp.Expect(HttpMethod.Get, DonationsUrl)
                .WithHeaders("Authorization", "Bearer " + AccessToken)
                .Respond("application/json", DonationsJson);

            var result = await _sut.GetDonationsAsync(OrganizationId, AccessToken, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().Equal(
                new OrganizationDonation("507f1f77bcf86cd799439021", DonationStatus.Active, 10, 4),
                new OrganizationDonation("507f1f77bcf86cd799439022", DonationStatus.Completed, 6, 6));
            _mockHttp.VerifyNoOutstandingExpectation();
        }

        [TestMethod]
        [DataRow("Pending", DonationStatus.Pending)]
        [DataRow("Active", DonationStatus.Active)]
        [DataRow("Completed", DonationStatus.Completed)]
        [DataRow("Cancelled", DonationStatus.Cancelled)]
        public async Task GetDonationsAsync_EveryStatus_IsMapped(string status, DonationStatus expected)
        {
            _mockHttp.When(DonationsUrl).Respond("application/json",
                $$"""{"donations":[{"id":"507f1f77bcf86cd799439041","projectId":"507f1f77bcf86cd799439021","status":"{{status}}","hoursCommitted":1,"hoursLogged":0,"progressPercentage":0,"createdAt":"2026-10-01T09:00:00+00:00"}]}""");

            var result = await _sut.GetDonationsAsync(OrganizationId, AccessToken, CancellationToken.None);

            result.Value.Should().ContainSingle().Which.Status.Should().Be(expected);
        }

        [TestMethod]
        public async Task GetContributionsAsync_Allowed_ReturnsTheContributionsAndSendsTheCallersToken()
        {
            _mockHttp.Expect(HttpMethod.Get, ContributionsUrl)
                .WithHeaders("Authorization", "Bearer " + AccessToken)
                .Respond("application/json", ContributionsJson);

            var result = await _sut.GetContributionsAsync(OrganizationId, AccessToken, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().Equal(
                new OrganizationContribution("507f1f77bcf86cd799439031", "507f1f77bcf86cd799439021", "octocat", 4, "https://github.com/acme/app/pull/7", new DateTimeOffset(2026, 10, 2, 9, 0, 0, TimeSpan.Zero)),
                new OrganizationContribution("507f1f77bcf86cd799439032", "507f1f77bcf86cd799439021", "octocat", 2, null, new DateTimeOffset(2026, 10, 3, 9, 0, 0, TimeSpan.Zero)));
            _mockHttp.VerifyNoOutstandingExpectation();
        }

        [TestMethod]
        [DataRow(HttpStatusCode.Unauthorized)]
        [DataRow(HttpStatusCode.Forbidden)]
        [DataRow(HttpStatusCode.NotFound)]
        public async Task GetDonationsAsync_DonationRefuses_ReturnsTheSameStatus(HttpStatusCode status)
        {
            _mockHttp.When(DonationsUrl).Respond(status);

            var result = await _sut.GetDonationsAsync(OrganizationId, AccessToken, CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            result.FailureStatusCode.Should().Be(status);
        }

        [TestMethod]
        [DataRow(HttpStatusCode.Unauthorized)]
        [DataRow(HttpStatusCode.Forbidden)]
        [DataRow(HttpStatusCode.NotFound)]
        public async Task GetContributionsAsync_DonationRefuses_ReturnsTheSameStatus(HttpStatusCode status)
        {
            _mockHttp.When(ContributionsUrl).Respond(status);

            var result = await _sut.GetContributionsAsync(OrganizationId, AccessToken, CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            result.FailureStatusCode.Should().Be(status);
        }

        [TestMethod]
        [DataRow(HttpStatusCode.InternalServerError)]
        [DataRow(HttpStatusCode.BadRequest)]
        [DataRow(HttpStatusCode.TooManyRequests)]
        public async Task GetDonationsAsync_DonationFails_ReturnsBadGateway(HttpStatusCode status)
        {
            _mockHttp.When(DonationsUrl).Respond(status);

            var result = await _sut.GetDonationsAsync(OrganizationId, AccessToken, CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            result.FailureStatusCode.Should().Be(HttpStatusCode.BadGateway);
        }

        [TestMethod]
        public async Task GetDonationsAsync_DonationUnreachable_ReturnsBadGateway()
        {
            _mockHttp.When(DonationsUrl).Throw(new HttpRequestException("connection refused"));

            var result = await _sut.GetDonationsAsync(OrganizationId, AccessToken, CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            result.FailureStatusCode.Should().Be(HttpStatusCode.BadGateway);
        }

        [TestMethod]
        public async Task GetContributionsAsync_DonationUnreachable_ReturnsBadGateway()
        {
            _mockHttp.When(ContributionsUrl).Throw(new HttpRequestException("connection refused"));

            var result = await _sut.GetContributionsAsync(OrganizationId, AccessToken, CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            result.FailureStatusCode.Should().Be(HttpStatusCode.BadGateway);
        }
    }
}