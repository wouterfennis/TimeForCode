using AwesomeAssertions;
using System.Net;
using System.Text;
using TimeForCode.Frontend.Bff.Sessions;

namespace TimeForCode.Frontend.Bff.Tests
{
    [TestClass]
    public sealed class AuthorizationApiTests
    {
        private const string TokensBody =
            """{"accessToken":{"token":"access","expiresAfter":"2030-01-01T00:00:00Z"},"refreshToken":{"token":"refresh","expiresAfter":"2030-02-01T00:00:00Z"},"isNewUser":true}""";

        public TestContext TestContext { get; set; } = null!;

        private sealed class StubHandler(HttpStatusCode status, string body) : HttpMessageHandler
        {
            public HttpRequestMessage? Request { get; private set; }

            public string? RequestBody { get; private set; }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                Request = request;
                RequestBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
                return new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
            }
        }

        private static (AuthorizationApi Sut, StubHandler Handler) Create(HttpStatusCode status, string body)
        {
            var handler = new StubHandler(status, body);
            return (new AuthorizationApi(new HttpClient(handler) { BaseAddress = new Uri("http://authorization.test/") }), handler);
        }

        [TestMethod]
        public async Task ExchangeCodeAsync_ValidCode_PostsCodeAndVerifierAndReturnsTokens()
        {
            var (sut, handler) = Create(HttpStatusCode.OK, TokensBody);

            var tokens = await sut.ExchangeCodeAsync("the-code", "the-verifier", TestContext.CancellationToken);

            tokens.Should().NotBeNull();
            tokens!.IsNewUser.Should().BeTrue();
            handler.Request!.RequestUri!.AbsolutePath.Should().Be("/api/v1/authentication/token");
            handler.RequestBody.Should().Contain("the-code").And.Contain("the-verifier");
        }

        [TestMethod]
        public async Task ExchangeCodeAsync_RejectedCode_ReturnsNull()
        {
            var (sut, _) = Create(HttpStatusCode.BadRequest, "{}");

            var tokens = await sut.ExchangeCodeAsync("bad", "verifier", TestContext.CancellationToken);

            tokens.Should().BeNull();
        }

        [TestMethod]
        public async Task ExchangeCodeAsync_ServerError_Throws()
        {
            var (sut, _) = Create(HttpStatusCode.InternalServerError, "{}");

            var act = () => sut.ExchangeCodeAsync("code", "verifier", TestContext.CancellationToken);

            await act.Should().ThrowAsync<HttpRequestException>();
        }

        [TestMethod]
        public async Task ExchangeCodeAsync_EmptyBody_Throws()
        {
            var (sut, _) = Create(HttpStatusCode.OK, "null");

            var act = () => sut.ExchangeCodeAsync("code", "verifier", TestContext.CancellationToken);

            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [TestMethod]
        public async Task RefreshAsync_ValidToken_PostsTheTokenAndReturnsNewTokens()
        {
            var (sut, handler) = Create(HttpStatusCode.OK, TokensBody);

            var tokens = await sut.RefreshAsync("old-refresh", TestContext.CancellationToken);

            tokens.Should().NotBeNull();
            handler.Request!.RequestUri!.AbsolutePath.Should().Be("/api/v1/authentication/refresh");
            handler.RequestBody.Should().Contain("old-refresh");
        }

        [TestMethod]
        public async Task RefreshAsync_RejectedToken_ReturnsNull()
        {
            var (sut, _) = Create(HttpStatusCode.BadRequest, "{}");

            var tokens = await sut.RefreshAsync("revoked", TestContext.CancellationToken);

            tokens.Should().BeNull();
        }

        [TestMethod]
        public async Task LogoutAsync_Success_PostsTheRefreshToken()
        {
            var (sut, handler) = Create(HttpStatusCode.NoContent, "");

            await sut.LogoutAsync("refresh", TestContext.CancellationToken);

            handler.Request!.RequestUri!.AbsolutePath.Should().Be("/api/v1/authentication/logout");
            handler.RequestBody.Should().Contain("refresh");
        }

        [TestMethod]
        public async Task LogoutAsync_ServerError_Throws()
        {
            var (sut, _) = Create(HttpStatusCode.BadGateway, "");

            var act = () => sut.LogoutAsync("refresh", TestContext.CancellationToken);

            await act.Should().ThrowAsync<HttpRequestException>();
        }

        private const string CeremonyBody = """{"ceremonyId":"ceremony-1","optionsJson":"{\"challenge\":\"abc\"}"}""";

        [TestMethod]
        public async Task AdminRegistrationOptionsAsync_ValidSecret_PostsTheSecretAndReturnsTheCeremony()
        {
            var (sut, handler) = Create(HttpStatusCode.OK, CeremonyBody);

            AdminCeremony? ceremony = await sut.AdminRegistrationOptionsAsync("the-secret", TestContext.CancellationToken);

            ceremony.Should().NotBeNull();
            ceremony!.CeremonyId.Should().Be("ceremony-1");
            ceremony.OptionsJson.Should().Contain("challenge");
            handler.Request!.RequestUri!.AbsolutePath.Should().Be("/api/v1/admin-authentication/registration/options");
            handler.RequestBody.Should().Contain("the-secret");
        }

        [TestMethod]
        public async Task AdminRegistrationOptionsAsync_RefusedRequest_ReturnsNull()
        {
            var (sut, _) = Create(HttpStatusCode.BadRequest, "{}");

            AdminCeremony? ceremony = await sut.AdminRegistrationOptionsAsync("wrong", TestContext.CancellationToken);

            ceremony.Should().BeNull();
        }

        [TestMethod]
        public async Task AdminRegistrationOptionsAsync_ServerError_Throws()
        {
            var (sut, _) = Create(HttpStatusCode.InternalServerError, "{}");

            var act = () => sut.AdminRegistrationOptionsAsync("secret", TestContext.CancellationToken);

            await act.Should().ThrowAsync<HttpRequestException>();
        }

        [TestMethod]
        public async Task AdminRegistrationOptionsAsync_EmptyBody_Throws()
        {
            var (sut, _) = Create(HttpStatusCode.OK, "null");

            var act = () => sut.AdminRegistrationOptionsAsync("secret", TestContext.CancellationToken);

            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [TestMethod]
        public async Task AdminCompleteRegistrationAsync_ValidCredential_PostsAllFieldsAndReturnsTokens()
        {
            var (sut, handler) = Create(HttpStatusCode.OK, TokensBody);

            IssuedTokens? tokens = await sut.AdminCompleteRegistrationAsync("the-secret", "the-credential", "ceremony-1", TestContext.CancellationToken);

            tokens.Should().NotBeNull();
            handler.Request!.RequestUri!.AbsolutePath.Should().Be("/api/v1/admin-authentication/registration");
            handler.RequestBody.Should().Contain("the-secret").And.Contain("the-credential").And.Contain("ceremony-1");
        }

        [TestMethod]
        public async Task AdminCompleteRegistrationAsync_RefusedRequest_ReturnsNull()
        {
            var (sut, _) = Create(HttpStatusCode.BadRequest, "{}");

            IssuedTokens? tokens = await sut.AdminCompleteRegistrationAsync("secret", "credential", "ceremony", TestContext.CancellationToken);

            tokens.Should().BeNull();
        }

        [TestMethod]
        public async Task AdminAuthenticationOptionsAsync_Success_ReturnsTheCeremony()
        {
            var (sut, handler) = Create(HttpStatusCode.OK, CeremonyBody);

            AdminCeremony? ceremony = await sut.AdminAuthenticationOptionsAsync(TestContext.CancellationToken);

            ceremony.Should().NotBeNull();
            ceremony!.CeremonyId.Should().Be("ceremony-1");
            handler.Request!.RequestUri!.AbsolutePath.Should().Be("/api/v1/admin-authentication/authentication/options");
        }

        [TestMethod]
        public async Task AdminAuthenticationOptionsAsync_RefusedRequest_ReturnsNull()
        {
            var (sut, _) = Create(HttpStatusCode.BadRequest, "{}");

            AdminCeremony? ceremony = await sut.AdminAuthenticationOptionsAsync(TestContext.CancellationToken);

            ceremony.Should().BeNull();
        }

        [TestMethod]
        public async Task AdminCompleteAuthenticationAsync_ValidAssertion_PostsCredentialAndCeremonyAndReturnsTokens()
        {
            var (sut, handler) = Create(HttpStatusCode.OK, TokensBody);

            IssuedTokens? tokens = await sut.AdminCompleteAuthenticationAsync("the-credential", "ceremony-1", TestContext.CancellationToken);

            tokens.Should().NotBeNull();
            handler.Request!.RequestUri!.AbsolutePath.Should().Be("/api/v1/admin-authentication/authentication");
            handler.RequestBody.Should().Contain("the-credential").And.Contain("ceremony-1");
        }

        [TestMethod]
        public async Task AdminCompleteAuthenticationAsync_RefusedRequest_ReturnsNull()
        {
            var (sut, _) = Create(HttpStatusCode.BadRequest, "{}");

            IssuedTokens? tokens = await sut.AdminCompleteAuthenticationAsync("credential", "ceremony", TestContext.CancellationToken);

            tokens.Should().BeNull();
        }
    }
}