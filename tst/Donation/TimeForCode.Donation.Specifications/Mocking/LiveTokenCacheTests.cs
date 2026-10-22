using FluentAssertions;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace TimeForCode.Donation.Specifications.Mocking
{
    [TestClass]
    public class LiveTokenCacheTests
    {
        private static readonly DateTime Now = new(2026, 10, 22, 12, 0, 0, DateTimeKind.Utc);

        [TestMethod]
        public async Task GetAsync_TokenStillValid_AcquiresOnce()
        {
            int acquired = 0;
            var cache = new LiveTokenCache(() => Task.FromResult(TokenExpiringAt(Now.AddMinutes(30), ++acquired)), () => Now);

            await cache.GetAsync();
            await cache.GetAsync();

            acquired.Should().Be(1);
        }

        [TestMethod]
        public async Task GetAsync_TokenAboutToExpire_AcquiresAgain()
        {
            int acquired = 0;
            var cache = new LiveTokenCache(() => Task.FromResult(TokenExpiringAt(Now.AddSeconds(30), ++acquired)), () => Now);

            await cache.GetAsync();
            await cache.GetAsync();

            acquired.Should().Be(2);
        }

        [TestMethod]
        public async Task GetAsync_PreviousAcquisitionFailed_AcquiresAgain()
        {
            int acquired = 0;
            var cache = new LiveTokenCache(
                () => ++acquired == 1
                    ? Task.FromException<string>(new InvalidOperationException("sign-in failed"))
                    : Task.FromResult(TokenExpiringAt(Now.AddMinutes(30), acquired)),
                () => Now);

            Func<Task> first = () => cache.GetAsync();
            await first.Should().ThrowAsync<InvalidOperationException>();

            (await cache.GetAsync()).Should().NotBeEmpty();
        }

        [TestMethod]
        public async Task GetAsync_UnreadableToken_IsNotKept()
        {
            int acquired = 0;
            var cache = new LiveTokenCache(
                () => Task.FromResult(++acquired == 1 ? "not-a-jwt" : TokenExpiringAt(Now.AddMinutes(30), acquired)),
                () => Now);

            Func<Task> first = () => cache.GetAsync();
            await first.Should().ThrowAsync<Exception>();

            (await cache.GetAsync()).Should().NotBe("not-a-jwt");
        }

        private static string TokenExpiringAt(DateTime expires, int sequence)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("a-signing-key-of-at-least-32-bytes!!"));
            var token = new JwtSecurityToken(
                claims: [new System.Security.Claims.Claim("seq", sequence.ToString())],
                notBefore: expires.AddHours(-1),
                expires: expires,
                signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}