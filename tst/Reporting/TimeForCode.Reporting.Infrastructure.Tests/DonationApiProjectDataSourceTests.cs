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
    public class DonationApiProjectDataSourceTests
    {
        private const string BaseUri = "http://donation.test/";
        private const string ProjectId = "507f1f77bcf86cd799439031";
        private const string AccessToken = "token-of-the-caller";
        private const string DownstreamSecret = "downstream-secret-detail";
        private const string DonationsUrl = BaseUri + "api/v1/project/" + ProjectId + "/donation";
        private const string ContributionsUrl = BaseUri + "api/v1/project/" + ProjectId + "/contribution";
        private const string ManageUrl = BaseUri + "api/v1/Project/" + ProjectId + "/manage";

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
              {"id":"507f1f77bcf86cd799439041","projectId":"507f1f77bcf86cd799439031","donorOrganizationId":"507f1f77bcf86cd799439013","status":"Active","hoursCommitted":10,"hoursLogged":4,"progressPercentage":40,"createdAt":"2026-10-01T09:00:00+00:00"},
              {"id":"507f1f77bcf86cd799439042","projectId":"507f1f77bcf86cd799439031","status":"Pending","hoursCommitted":6,"hoursLogged":0,"progressPercentage":0,"createdAt":"2026-10-02T09:00:00+00:00"}
            ]}
            """;

        private const string ContributionsJson = """
            {"contributions":[
              {"id":"507f1f77bcf86cd799439051","projectId":"507f1f77bcf86cd799439031","contributorId":"507f1f77bcf86cd799439061","contributorGitHubLogin":"octocat","contributorAvatarUrl":"https://avatars.githubusercontent.com/u/1","hours":4,"loggedAt":"2026-10-02T09:00:00+00:00"}
            ]}
            """;

        private const string MaintainedProjectJson = """
            {"project":{"id":"507f1f77bcf86cd799439031","name":"app","fullName":"acme/app","githubUrl":"https://github.com/acme/app","topics":[],"defaultBranch":"main","ownerLogin":"acme","ownerAvatarUrl":"https://avatars.githubusercontent.com/u/2","createdAt":"2026-01-01T00:00:00+00:00","updatedAt":"2026-01-01T00:00:00+00:00","pushedAt":"2026-01-01T00:00:00+00:00","status":"Active"}}
            """;

        [TestMethod]
        public async Task GetProjectDonationsAsync_PublishedProject_ReturnsTheDonationsWithoutAToken()
        {
            _mockHttp.Expect(HttpMethod.Get, DonationsUrl)
                .With(request => request.Headers.Authorization is null)
                .Respond("application/json", DonationsJson);

            var result = await _sut.GetProjectDonationsAsync(ProjectId, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().Equal(
                new ProjectDonation("507f1f77bcf86cd799439041", "507f1f77bcf86cd799439013", DonationStatus.Active, 10, 4, new DateTimeOffset(2026, 10, 1, 9, 0, 0, TimeSpan.Zero)),
                new ProjectDonation("507f1f77bcf86cd799439042", null, DonationStatus.Pending, 6, 0, new DateTimeOffset(2026, 10, 2, 9, 0, 0, TimeSpan.Zero)));
            _mockHttp.VerifyNoOutstandingExpectation();
        }

        [TestMethod]
        public async Task GetProjectDonationsAsync_ProjectNotFound_ReturnsNotFound()
        {
            _mockHttp.When(DonationsUrl).Respond(HttpStatusCode.NotFound);

            var result = await _sut.GetProjectDonationsAsync(ProjectId, CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            result.FailureStatusCode.Should().Be(HttpStatusCode.NotFound);
            result.ErrorMessage.Should().Be("Project not found.");
        }

        [TestMethod]
        public async Task GetProjectDonationsAsync_DonationFailsWithABody_ReturnsBadGatewayWithoutTheBody()
        {
            _mockHttp.When(DonationsUrl).Respond(HttpStatusCode.InternalServerError, "application/json", $$"""{"detail":"{{DownstreamSecret}}"}""");

            var result = await _sut.GetProjectDonationsAsync(ProjectId, CancellationToken.None);

            result.FailureStatusCode.Should().Be(HttpStatusCode.BadGateway);
            result.ErrorMessage.Should().NotContain(DownstreamSecret);
        }

        [TestMethod]
        public async Task GetProjectDonationsAsync_DonationUnreachable_ReturnsBadGateway()
        {
            _mockHttp.When(DonationsUrl).Throw(new HttpRequestException("connection refused"));

            var result = await _sut.GetProjectDonationsAsync(ProjectId, CancellationToken.None);

            result.FailureStatusCode.Should().Be(HttpStatusCode.BadGateway);
        }

        [TestMethod]
        public async Task GetProjectContributionsAsync_PublishedProject_ReturnsTheContributions()
        {
            _mockHttp.Expect(HttpMethod.Get, ContributionsUrl).Respond("application/json", ContributionsJson);

            var result = await _sut.GetProjectContributionsAsync(ProjectId, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().Equal(new ProjectContribution("507f1f77bcf86cd799439061", "octocat", 4));
            _mockHttp.VerifyNoOutstandingExpectation();
        }

        [TestMethod]
        public async Task GetProjectContributionsAsync_ProjectNotFound_ReturnsNotFound()
        {
            _mockHttp.When(ContributionsUrl).Respond(HttpStatusCode.NotFound);

            var result = await _sut.GetProjectContributionsAsync(ProjectId, CancellationToken.None);

            result.FailureStatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task GetProjectContributionsAsync_DonationUnreachable_ReturnsBadGateway()
        {
            _mockHttp.When(ContributionsUrl).Throw(new HttpRequestException("connection refused"));

            var result = await _sut.GetProjectContributionsAsync(ProjectId, CancellationToken.None);

            result.FailureStatusCode.Should().Be(HttpStatusCode.BadGateway);
        }

        [TestMethod]
        public async Task VerifyMaintainerAsync_Maintainer_SucceedsAndSendsTheCallersToken()
        {
            _mockHttp.Expect(HttpMethod.Get, ManageUrl)
                .WithHeaders("Authorization", "Bearer " + AccessToken)
                .Respond("application/json", MaintainedProjectJson);

            var result = await _sut.VerifyMaintainerAsync(ProjectId, AccessToken, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            _mockHttp.VerifyNoOutstandingExpectation();
        }

        [TestMethod]
        [DataRow(HttpStatusCode.Unauthorized)]
        [DataRow(HttpStatusCode.Forbidden)]
        [DataRow(HttpStatusCode.NotFound)]
        public async Task VerifyMaintainerAsync_DonationRefuses_ReturnsTheSameStatus(HttpStatusCode status)
        {
            _mockHttp.When(ManageUrl).Respond(status);

            var result = await _sut.VerifyMaintainerAsync(ProjectId, AccessToken, CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            result.FailureStatusCode.Should().Be(status);
        }

        [TestMethod]
        public async Task VerifyMaintainerAsync_NotTheMaintainer_ExplainsWhoMayRead()
        {
            _mockHttp.When(ManageUrl).Respond(HttpStatusCode.Forbidden, "application/json", $$"""{"detail":"{{DownstreamSecret}}"}""");

            var result = await _sut.VerifyMaintainerAsync(ProjectId, AccessToken, CancellationToken.None);

            result.ErrorMessage.Should().Be("Only the maintainer of the project or an administrator can read its donation report.");
            result.ErrorMessage.Should().NotContain(DownstreamSecret);
        }

        [TestMethod]
        public async Task VerifyMaintainerAsync_DonationFails_ReturnsBadGateway()
        {
            _mockHttp.When(ManageUrl).Respond(HttpStatusCode.InternalServerError);

            var result = await _sut.VerifyMaintainerAsync(ProjectId, AccessToken, CancellationToken.None);

            result.FailureStatusCode.Should().Be(HttpStatusCode.BadGateway);
        }

        [TestMethod]
        public async Task VerifyMaintainerAsync_DonationUnreachable_ReturnsBadGateway()
        {
            _mockHttp.When(ManageUrl).Throw(new HttpRequestException("connection refused"));

            var result = await _sut.VerifyMaintainerAsync(ProjectId, AccessToken, CancellationToken.None);

            result.FailureStatusCode.Should().Be(HttpStatusCode.BadGateway);
        }
    }
}