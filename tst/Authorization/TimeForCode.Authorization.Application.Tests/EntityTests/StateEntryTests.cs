using FluentAssertions;
using TimeForCode.Authorization.Domain.Entities;
using TimeForCode.Authorization.Values;

namespace TimeForCode.Authorization.Application.Tests.EntityTests
{
    [TestClass]
    public class StateEntryTests
    {
        private static readonly DateTimeOffset Now = new(2026, 10, 18, 12, 0, 0, TimeSpan.Zero);

        [TestMethod]
        public void Start_NewLoginAttempt_ExpiresAfterTheStateLifetime()
        {
            var redirectUri = new Uri("https://example.test/callback");

            var entry = StateEntry.Start("key", IdentityProvider.Github, redirectUri, "verifier", Now);

            entry.Key.Should().Be("key");
            entry.IdentityProvider.Should().Be(IdentityProvider.Github);
            entry.RedirectUri.Should().Be(redirectUri);
            entry.CodeVerifier.Should().Be("verifier");
            entry.ExpiresAfter.Should().Be(Now.Add(StateEntry.Lifetime));
            entry.Id.Should().NotBe(default);
        }

        [TestMethod]
        public void IsExpired_AfterTheLifetime_IsTrue()
        {
            var entry = StateEntry.Start("key", IdentityProvider.Github, new Uri("https://example.test/callback"), "verifier", Now);

            entry.IsExpired(Now.Add(StateEntry.Lifetime).AddSeconds(1)).Should().BeTrue();
            entry.IsExpired(Now.AddMinutes(1)).Should().BeFalse();
        }
    }
}