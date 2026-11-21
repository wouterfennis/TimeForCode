using AwesomeAssertions;
using System.Security.Claims;
using TimeForCode.Donation.Api.V1;
using TimeForCode.Donation.Values;

namespace TimeForCode.Donation.Api.Tests
{
    /// <summary>
    /// Builds an <see cref="Actor"/> from the claims of the signed-in caller.
    /// </summary>
    [TestClass]
    public class ClaimsPrincipalExtensionsTests
    {
        private static ClaimsPrincipal Principal(params Claim[] claims)
        {
            return new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
        }

        [TestMethod]
        public void TryGetActor_UserWithoutAdminScope_ReturnsNonAdministrator()
        {
            var principal = Principal(new Claim(ClaimTypes.NameIdentifier, "user-1"));

            principal.TryGetActor(out var actor).Should().BeTrue();

            actor.Should().Be(new Actor("user-1", false));
        }

        [TestMethod]
        public void TryGetActor_UserWithAdminScope_ReturnsAdministrator()
        {
            var principal = Principal(new Claim(ClaimTypes.NameIdentifier, "admin-1"), new Claim("scope", "admin"));

            principal.TryGetActor(out var actor).Should().BeTrue();

            actor.Should().Be(new Actor("admin-1", true));
        }

        [TestMethod]
        public void TryGetActor_NoUserIdentifier_ReturnsFalse()
        {
            Principal(new Claim("scope", "admin")).TryGetActor(out var actor).Should().BeFalse();

            actor.Should().BeNull();
        }
    }
}