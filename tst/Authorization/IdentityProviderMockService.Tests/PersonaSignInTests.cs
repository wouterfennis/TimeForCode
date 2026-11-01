using AwesomeAssertions;
using System.Text.Json;

namespace IdentityProviderMockService.Tests
{
    [TestClass]
    public class PersonaSignInTests
    {
        [TestMethod]
        public async Task SignIn_DifferentPersonas_ReturnDifferentIdAndLogin()
        {
            using var mock = new MockSignIn();

            JsonElement alice = await mock.SignInAsync("alice");
            JsonElement bob = await mock.SignInAsync("bob");

            alice.GetProperty("id").GetUInt32().Should().NotBe(bob.GetProperty("id").GetUInt32());
            alice.GetProperty("login").GetString().Should().NotBe(bob.GetProperty("login").GetString());
        }

        [TestMethod]
        public async Task SignIn_SamePersonaTwice_ReturnsSameIdAndLogin()
        {
            using var mock = new MockSignIn();

            JsonElement first = await mock.SignInAsync("alice");
            JsonElement second = await mock.SignInAsync("alice");

            second.GetProperty("id").GetUInt32().Should().Be(first.GetProperty("id").GetUInt32());
            second.GetProperty("login").GetString().Should().Be(first.GetProperty("login").GetString());
        }

        [TestMethod]
        public async Task SignIn_Persona_UsesPersonaAsLogin()
        {
            using var mock = new MockSignIn();

            JsonElement alice = await mock.SignInAsync("alice");

            alice.GetProperty("login").GetString().Should().Be("alice");
        }

        [TestMethod]
        public async Task SignIn_WithoutPersona_ReturnsDefaultJohnDoe()
        {
            using var mock = new MockSignIn();

            JsonElement user = await mock.SignInAsync(null);

            user.GetProperty("id").GetUInt32().Should().Be(1u);
            user.GetProperty("login").GetString().Should().Be("johndoe");
        }

        [TestMethod]
        public async Task GetAuthorize_WithPersona_ShowsPersonaInsteadOfMockUser()
        {
            using var mock = new MockSignIn();

            string page = await mock.GetAuthorizePageAsync("alice");

            page.Should().Contain("<strong>alice</strong>").And.NotContain("mock-user");
        }

        [TestMethod]
        public async Task GetAuthorize_WithoutPersona_ShowsMockUser()
        {
            using var mock = new MockSignIn();

            string page = await mock.GetAuthorizePageAsync(null);

            page.Should().Contain("mock-user");
        }

        [TestMethod]
        public async Task GetAuthorize_PersonaWithMarkup_IsHtmlEncoded()
        {
            using var mock = new MockSignIn();

            string page = await mock.GetAuthorizePageAsync("<script>x</script>");

            page.Should().NotContain("<script>x</script>");
        }
    }
}