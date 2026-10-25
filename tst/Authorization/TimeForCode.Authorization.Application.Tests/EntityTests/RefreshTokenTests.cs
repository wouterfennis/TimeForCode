using AwesomeAssertions;
using TimeForCode.Authorization.Domain.Entities;

namespace TimeForCode.Authorization.Application.Tests.EntityTests
{
    [TestClass]
    public class RefreshTokenTests
    {
        private static readonly DateTimeOffset Now = new(2026, 10, 18, 12, 0, 0, TimeSpan.Zero);

        [TestMethod]
        public void Issue_NewToken_ExpiresAfterTheGivenLifetimeAndIsNotRotated()
        {
            var token = RefreshToken.Issue("hash", "user-1", Now, 7);

            token.TokenHash.Should().Be("hash");
            token.UserId.Should().Be("user-1");
            token.ExpiresAfter.Should().Be(Now.AddDays(7));
            token.IsRotated.Should().BeFalse();
            token.Id.Should().NotBe(default);
        }

        [TestMethod]
        public void CheckReplaceable_FreshToken_IsAllowed()
        {
            var token = RefreshToken.Issue("hash", "user-1", Now, 7);

            token.CheckReplaceable(Now.AddDays(1)).Should().Be(RefreshTokenReplacementCheck.Allowed);
        }

        [TestMethod]
        public void CheckReplaceable_ExpiredToken_IsExpired()
        {
            var token = RefreshToken.Issue("hash", "user-1", Now, 7);

            token.CheckReplaceable(Now.AddDays(8)).Should().Be(RefreshTokenReplacementCheck.Expired);
        }

        [TestMethod]
        public void CheckReplaceable_RotatedToken_IsReplayed()
        {
            var token = RefreshToken.Issue("hash", "user-1", Now, 7);
            token.MarkRotated(Now.AddHours(1));

            token.CheckReplaceable(Now.AddDays(1)).Should().Be(RefreshTokenReplacementCheck.Replayed);
        }

        [TestMethod]
        public void CheckReplaceable_ExpiredAndRotatedToken_IsExpired()
        {
            var token = RefreshToken.Issue("hash", "user-1", Now, 7);
            token.MarkRotated(Now.AddHours(1));

            token.CheckReplaceable(Now.AddDays(8)).Should().Be(RefreshTokenReplacementCheck.Expired);
        }

        [TestMethod]
        [DataRow(nameof(RefreshToken.ExpiresAfter))]
        [DataRow(nameof(RefreshToken.RotatedAt))]
        public void LifecycleProperty_FromOutsideTheEntity_CannotBeAssigned(string propertyName)
        {
            var setter = typeof(RefreshToken).GetProperty(propertyName)!.SetMethod;

            (setter == null || !setter.IsPublic || setter.ReturnParameter.GetRequiredCustomModifiers().Length > 0)
                .Should().BeTrue($"{propertyName} may only change through the entity's own methods");
        }
    }
}