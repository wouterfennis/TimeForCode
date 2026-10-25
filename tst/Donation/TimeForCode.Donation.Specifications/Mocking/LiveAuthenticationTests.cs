using AwesomeAssertions;

namespace TimeForCode.Donation.Specifications.Mocking
{
    [TestClass]
    public class LiveAuthenticationTests
    {
        private const string ConfirmForm = """
            <form method="post" action="/login/oauth/confirm">
              <input type="hidden" name="state"        value="abc&amp;123" />
              <input type="hidden" name="redirect_uri" value="http://localhost:8080/api/v1/authentication/callback" />
              <input type="hidden" name="scope"        value="" />
            </form>
            """;

        [TestMethod]
        public void ExtractInputValue_NamedInput_ReturnsDecodedValue()
        {
            LiveAuthentication.ExtractInputValue(ConfirmForm, "state").Should().Be("abc&123");
        }

        [TestMethod]
        public void ExtractInputValue_EmptyValue_ReturnsEmptyString()
        {
            LiveAuthentication.ExtractInputValue(ConfirmForm, "scope").Should().BeEmpty();
        }

        [TestMethod]
        public void ExtractInputValue_AttributesWithNameSuffix_AreIgnored()
        {
            const string html = """<input data-name="wrong" name="state" aria-value="wrong" value="right" />""";

            LiveAuthentication.ExtractInputValue(html, "state").Should().Be("right");
        }

        [TestMethod]
        public void ExtractInputValue_UnknownInput_Throws()
        {
            Action act = () => LiveAuthentication.ExtractInputValue(ConfirmForm, "client_id");

            act.Should().Throw<InvalidOperationException>().WithMessage("*client_id*");
        }

        [TestMethod]
        public void ReadAccessToken_CookieWithTokenJson_ReturnsToken()
        {
            string[] cookies =
            [
                "RefreshToken=%7B%22token%22%3A%22refresh%22%7D; expires=Thu, 01 Jan 2099 00:00:00 GMT; path=/",
                "AccessToken=%7B%22Token%22%3A%22jwt-value%22%2C%22ExpiresAfter%22%3A%222099-01-01T00%3A00%3A00Z%22%7D; path=/; httponly"
            ];

            LiveAuthentication.ReadAccessToken(cookies).Should().Be("jwt-value");
        }

        [TestMethod]
        public void ReadAccessToken_EmptyCookieBeforeRealOne_SkipsTheEmptyCookie()
        {
            string[] cookies =
            [
                "AccessToken=; expires=Thu, 01 Jan 1970 00:00:00 GMT; path=/",
                "AccessToken=%7B%22Token%22%3A%22jwt-value%22%2C%22ExpiresAfter%22%3A%222099-01-01T00%3A00%3A00Z%22%7D; path=/"
            ];

            LiveAuthentication.ReadAccessToken(cookies).Should().Be("jwt-value");
        }

        [TestMethod]
        public void ReadAccessToken_NoAccessTokenCookie_Throws()
        {
            Action act = () => LiveAuthentication.ReadAccessToken(["RefreshToken=x; path=/"]);

            act.Should().Throw<InvalidOperationException>().WithMessage("*AccessToken*");
        }

        [TestMethod]
        public void ResolveAuthorizationAddress_NotConfigured_ThrowsNamingTheVariable()
        {
            Action act = () => LiveAuthentication.ResolveAuthorizationAddress(null);

            act.Should().Throw<InvalidOperationException>().WithMessage($"*{LiveAuthentication.AuthorizationUrlVariable}*");
        }

        [TestMethod]
        public void ResolveAuthorizationAddress_AbsoluteUrl_ReturnsUri()
        {
            LiveAuthentication.ResolveAuthorizationAddress("http://localhost:8080")
                .Should().Be(new Uri("http://localhost:8080"));
        }

        [TestMethod]
        public void ResolveTestAdminSecret_NotConfigured_ThrowsNamingTheVariable()
        {
            Action act = () => LiveAuthentication.ResolveTestAdminSecret("  ");

            act.Should().Throw<InvalidOperationException>().WithMessage($"*{LiveAuthentication.TestAdminSecretVariable}*");
        }

        [TestMethod]
        public void ResolveClientRedirectUri_NotConfigured_ReturnsLocalFrontend()
        {
            LiveAuthentication.ResolveClientRedirectUri(null).Should().Be(new Uri("http://localhost:8084"));
        }
    }
}