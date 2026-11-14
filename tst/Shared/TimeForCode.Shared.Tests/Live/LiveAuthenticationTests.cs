using AwesomeAssertions;
using TimeForCode.Shared.Live;

namespace TimeForCode.Shared.Tests.Live
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
        public void ReadCode_CallbackRedirectWithCode_ReturnsTheCode()
        {
            var redirect = new Uri("http://localhost:8084/?code=one-time%2Bcode");

            LiveAuthentication.ReadCode(redirect).Should().Be("one-time+code");
        }

        [TestMethod]
        public void ReadCode_RedirectWithoutCode_Throws()
        {
            Action act = () => LiveAuthentication.ReadCode(new Uri("http://localhost:8084/?error=denied"));

            act.Should().Throw<InvalidOperationException>().WithMessage("*code*");
        }

        [TestMethod]
        public void CreatePkcePair_Always_ChallengeIsTheS256OfTheVerifier()
        {
            var (verifier, challenge) = LiveAuthentication.CreatePkcePair();

            verifier.Length.Should().BeInRange(43, 128);
            challenge.Should().MatchRegex("^[A-Za-z0-9_-]{43}$");
            Convert.ToBase64String(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.ASCII.GetBytes(verifier)))
                .TrimEnd('=').Replace('+', '-').Replace('/', '_').Should().Be(challenge);
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
        public void AddPersona_NoPersona_ReturnsSameUrl()
        {
            var url = new Uri("http://localhost:8081/login/oauth/authorize?state=s&scope=user");

            LiveAuthentication.AddPersona(url, null).Should().Be(url);
        }

        [TestMethod]
        public void AddPersona_Persona_AppendsEscapedQueryParameter()
        {
            var url = new Uri("http://localhost:8081/login/oauth/authorize?state=s&scope=user");

            Uri result = LiveAuthentication.AddPersona(url, "Alice Smith");

            result.AbsoluteUri.Should().Be("http://localhost:8081/login/oauth/authorize?state=s&scope=user&persona=Alice%20Smith");
        }

        [TestMethod]
        public void AddPersona_UrlWithoutQuery_StartsTheQuery()
        {
            var url = new Uri("http://localhost:8081/login/oauth/authorize");

            LiveAuthentication.AddPersona(url, "alice").AbsoluteUri.Should().Be("http://localhost:8081/login/oauth/authorize?persona=alice");
        }

        [TestMethod]
        public void TryExtractInputValue_UnknownInput_ReturnsNull()
        {
            LiveAuthentication.TryExtractInputValue(ConfirmForm, "persona").Should().BeNull();
        }

        [TestMethod]
        public void TryExtractInputValue_NamedInput_ReturnsDecodedValue()
        {
            LiveAuthentication.TryExtractInputValue(ConfirmForm, "state").Should().Be("abc&123");
        }

        [TestMethod]
        public void ResolveClientRedirectUri_NotConfigured_ReturnsLocalFrontend()
        {
            LiveAuthentication.ResolveClientRedirectUri(null).Should().Be(new Uri("http://localhost:8084"));
        }
    }
}