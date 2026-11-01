using AwesomeAssertions;
using System.Net;
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
        public async Task GetAuthorize_PersonaWithMarkup_IsRejected()
        {
            using var mock = new MockSignIn();

            HttpStatusCode status = await mock.GetAuthorizeStatusAsync("<script>x</script>");

            status.Should().Be(HttpStatusCode.BadRequest);
        }

        [TestMethod]
        public async Task GetAuthorize_PersonaLongerThanLimit_IsRejected()
        {
            using var mock = new MockSignIn();

            HttpStatusCode status = await mock.GetAuthorizeStatusAsync(new string('a', 65));

            status.Should().Be(HttpStatusCode.BadRequest);
        }

        [TestMethod]
        public async Task SignIn_PersonaDifferingInCaseAndSpacing_ReturnsSameIdAndLogin()
        {
            using var mock = new MockSignIn();

            JsonElement lower = await mock.SignInAsync("alice");
            JsonElement mixed = await mock.SignInAsync(" Alice ");

            mixed.GetProperty("id").GetUInt32().Should().Be(lower.GetProperty("id").GetUInt32());
            mixed.GetProperty("login").GetString().Should().Be(lower.GetProperty("login").GetString());
        }

        [TestMethod]
        public async Task SignIn_WhitespaceOnlyPersona_BehavesLikeNoPersona()
        {
            using var mock = new MockSignIn();

            JsonElement user = await mock.SignInAsync("   ");

            user.GetProperty("id").GetUInt32().Should().Be(1u);
            user.GetProperty("login").GetString().Should().Be("johndoe");
        }

        [TestMethod]
        public async Task GetAuthorize_WhitespaceOnlyPersona_ShowsMockUser()
        {
            using var mock = new MockSignIn();

            string page = await mock.GetAuthorizePageAsync("   ");

            page.Should().Contain("mock-user");
        }

        [TestMethod]
        public async Task SignIn_WithoutPersona_KeepsOriginalNodeIdAndAvatar()
        {
            using var mock = new MockSignIn();

            JsonElement user = await mock.SignInAsync(null);

            user.GetProperty("node_id").GetString().Should().Be("MDQ6VXNlcjE=");
            user.GetProperty("avatar_url").GetString().Should().Be("https://avatars.githubusercontent.com/u/1?v=4");
        }

        [TestMethod]
        public async Task SignIn_TwentyPersonas_GetDistinctIds()
        {
            using var mock = new MockSignIn();
            var ids = new HashSet<uint>();

            for (int i = 0; i < 20; i++)
            {
                ids.Add((await mock.SignInAsync($"persona-{i}")).GetProperty("id").GetUInt32());
            }

            ids.Should().HaveCount(20);
        }
    }
}