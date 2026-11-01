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
        private const string DonationsUrl = BaseUri + "api/v1/donororganization/" + OrganizationId + "/donation*";
        private const string ContributionsUrl = BaseUri + "api/v1/donororganization/" + OrganizationId + "/contribution*";

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
            ],"totalCount":2,"pageNumber":1,"pageSize":100}
            """;

        private const string ContributionsJson = """
            {"contributions":[
              {"id":"507f1f77bcf86cd799439031","projectId":"507f1f77bcf86cd799439021","contributorId":"507f1f77bcf86cd799439051","contributorGitHubLogin":"octocat","contributorAvatarUrl":"https://avatars.githubusercontent.com/u/1","hours":4,"githubReference":"https://github.com/acme/app/pull/7","loggedAt":"2026-10-02T09:00:00+00:00"},
              {"id":"507f1f77bcf86cd799439032","projectId":"507f1f77bcf86cd799439021","contributorId":"507f1f77bcf86cd799439051","contributorGitHubLogin":"octocat","contributorAvatarUrl":"https://avatars.githubusercontent.com/u/1","hours":2,"loggedAt":"2026-10-03T09:00:00+00:00"}
            ],"totalCount":2,"pageNumber":1,"pageSize":100}
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

        private static string DonationPage(int pageNumber, int totalCount, params string[] projectIds)
        {
            var donations = string.Join(",", projectIds.Select(projectId =>
                $$"""{"id":"507f1f77bcf86cd799439041","projectId":"{{projectId}}","status":"Active","hoursCommitted":1,"hoursLogged":0,"progressPercentage":0,"createdAt":"2026-10-01T09:00:00+00:00"}"""));
            return $$"""{"donations":[{{donations}}],"totalCount":{{totalCount}},"pageNumber":{{pageNumber}},"pageSize":100}""";
        }

        private static string ContributionPage(int pageNumber, int totalCount, params string[] contributionIds)
        {
            var contributions = string.Join(",", contributionIds.Select(id =>
                $$"""{"id":"{{id}}","projectId":"507f1f77bcf86cd799439021","contributorId":"507f1f77bcf86cd799439051","contributorGitHubLogin":"octocat","contributorAvatarUrl":"https://avatars.githubusercontent.com/u/1","hours":1,"loggedAt":"2026-10-02T09:00:00+00:00"}"""));
            return $$"""{"contributions":[{{contributions}}],"totalCount":{{totalCount}},"pageNumber":{{pageNumber}},"pageSize":100}""";
        }

        [TestMethod]
        public async Task GetDonationsAsync_MoreThanOnePage_ReturnsTheDonationsOfEveryPage()
        {
            _mockHttp.Expect(HttpMethod.Get, DonationsUrl).WithQueryString("pageNumber", "1").WithQueryString("pageSize", "100")
                .Respond("application/json", DonationPage(1, 3, "507f1f77bcf86cd799439021", "507f1f77bcf86cd799439022"));
            _mockHttp.Expect(HttpMethod.Get, DonationsUrl).WithQueryString("pageNumber", "2").WithQueryString("pageSize", "100")
                .Respond("application/json", DonationPage(2, 3, "507f1f77bcf86cd799439023"));

            var result = await _sut.GetDonationsAsync(OrganizationId, AccessToken, CancellationToken.None);

            result.Value.Select(d => d.ProjectId).Should().Equal("507f1f77bcf86cd799439021", "507f1f77bcf86cd799439022", "507f1f77bcf86cd799439023");
            _mockHttp.VerifyNoOutstandingExpectation();
        }

        [TestMethod]
        public async Task GetDonationsAsync_LaterPageFails_ReturnsBadGateway()
        {
            _mockHttp.Expect(HttpMethod.Get, DonationsUrl).WithQueryString("pageNumber", "1")
                .Respond("application/json", DonationPage(1, 2, "507f1f77bcf86cd799439021"));
            _mockHttp.Expect(HttpMethod.Get, DonationsUrl).WithQueryString("pageNumber", "2")
                .Respond(HttpStatusCode.InternalServerError);

            var result = await _sut.GetDonationsAsync(OrganizationId, AccessToken, CancellationToken.None);

            result.FailureStatusCode.Should().Be(HttpStatusCode.BadGateway);
        }

        [TestMethod]
        public async Task GetContributionsAsync_MoreThanOnePage_ReturnsTheContributionsOfEveryPage()
        {
            _mockHttp.Expect(HttpMethod.Get, ContributionsUrl).WithQueryString("pageNumber", "1").WithQueryString("pageSize", "100")
                .Respond("application/json", ContributionPage(1, 3, "507f1f77bcf86cd799439031", "507f1f77bcf86cd799439032"));
            _mockHttp.Expect(HttpMethod.Get, ContributionsUrl).WithQueryString("pageNumber", "2").WithQueryString("pageSize", "100")
                .Respond("application/json", ContributionPage(2, 3, "507f1f77bcf86cd799439033"));

            var result = await _sut.GetContributionsAsync(OrganizationId, AccessToken, CancellationToken.None);

            result.Value.Select(c => c.Id).Should().Equal("507f1f77bcf86cd799439031", "507f1f77bcf86cd799439032", "507f1f77bcf86cd799439033");
            _mockHttp.VerifyNoOutstandingExpectation();
        }

        [TestMethod]
        public async Task GetContributionsAsync_PageIsEmptyBeforeTheTotalIsReached_StopsAskingForMore()
        {
            var requests = 0;
            _mockHttp.When(ContributionsUrl).Respond(_ =>
            {
                requests++;
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(ContributionPage(requests, 5), System.Text.Encoding.UTF8, "application/json")
                };
            });

            var result = await _sut.GetContributionsAsync(OrganizationId, AccessToken, CancellationToken.None);

            result.Value.Should().BeEmpty();
            requests.Should().Be(1);
        }

        [TestMethod]
        [DataRow("Pending", DonationStatus.Pending)]
        [DataRow("Active", DonationStatus.Active)]
        [DataRow("Completed", DonationStatus.Completed)]
        [DataRow("Cancelled", DonationStatus.Cancelled)]
        public async Task GetDonationsAsync_EveryStatus_IsMapped(string status, DonationStatus expected)
        {
            _mockHttp.When(DonationsUrl).Respond("application/json",
                $$"""{"donations":[{"id":"507f1f77bcf86cd799439041","projectId":"507f1f77bcf86cd799439021","status":"{{status}}","hoursCommitted":1,"hoursLogged":0,"progressPercentage":0,"createdAt":"2026-10-01T09:00:00+00:00"}],"totalCount":1,"pageNumber":1,"pageSize":100}""");

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

        private const string DownstreamSecret = "downstream-internal-detail";

        [TestMethod]
        [DataRow(HttpStatusCode.Unauthorized, "The Donation service did not accept the access token.")]
        [DataRow(HttpStatusCode.Forbidden, "Only members of the donor organization or an administrator can read its data.")]
        [DataRow(HttpStatusCode.NotFound, "Donor organization not found.")]
        public async Task GetDonationsAsync_DonationRefusesWithABody_ReturnsAFixedMessageWithoutTheDownstreamBody(HttpStatusCode status, string expectedMessage)
        {
            _mockHttp.When(DonationsUrl).Respond(status, "application/json", $$"""{"detail":"{{DownstreamSecret}}"}""");

            var result = await _sut.GetDonationsAsync(OrganizationId, AccessToken, CancellationToken.None);

            result.ErrorMessage.Should().Be(expectedMessage);
            result.ErrorMessage.Should().NotContain(DownstreamSecret);
        }

        [TestMethod]
        [DataRow(HttpStatusCode.Unauthorized, "The Donation service did not accept the access token.")]
        [DataRow(HttpStatusCode.Forbidden, "Only members of the donor organization or an administrator can read its data.")]
        [DataRow(HttpStatusCode.NotFound, "Donor organization not found.")]
        public async Task GetContributionsAsync_DonationRefusesWithABody_ReturnsAFixedMessageWithoutTheDownstreamBody(HttpStatusCode status, string expectedMessage)
        {
            _mockHttp.When(ContributionsUrl).Respond(status, "application/json", $$"""{"detail":"{{DownstreamSecret}}"}""");

            var result = await _sut.GetContributionsAsync(OrganizationId, AccessToken, CancellationToken.None);

            result.ErrorMessage.Should().Be(expectedMessage);
            result.ErrorMessage.Should().NotContain(DownstreamSecret);
        }

        [TestMethod]
        public async Task GetDonationsAsync_DonationFailsWithABody_DoesNotLeakTheBody()
        {
            _mockHttp.When(DonationsUrl).Respond(HttpStatusCode.InternalServerError, "application/json", $$"""{"detail":"{{DownstreamSecret}}"}""");

            var result = await _sut.GetDonationsAsync(OrganizationId, AccessToken, CancellationToken.None);

            result.ErrorMessage.Should().NotContain(DownstreamSecret);
        }

        [TestMethod]
        public async Task GetDonationsAndContributions_Always_OnlyReadFromTheDonationApi()
        {
            var methods = new List<HttpMethod>();
            _mockHttp.When("*").Respond(request =>
            {
                methods.Add(request.Method);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("""{"donations":[],"contributions":[],"totalCount":0,"pageNumber":1,"pageSize":100}""", System.Text.Encoding.UTF8, "application/json")
                };
            });

            await _sut.GetDonationsAsync(OrganizationId, AccessToken, CancellationToken.None);
            await _sut.GetContributionsAsync(OrganizationId, AccessToken, CancellationToken.None);

            methods.Should().HaveCount(2).And.OnlyContain(method => method == HttpMethod.Get);
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