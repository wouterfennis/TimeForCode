using AwesomeAssertions;
using TimeForCode.Authorization.Application.Options;

namespace TimeForCode.Authorization.Application.Tests.Options
{
    [TestClass]
    public class TokenCreationOptionsTests
    {
        private static TokenCreationOptions Options(int minutes) => new()
        {
            Issuer = "issuer",
            Audiences = ["audience"],
            TokenExpiresInMinutes = minutes,
            DefaultRefreshTokenExpirationAfterInDays = 7
        };

        [TestMethod]
        [DataRow(1)]
        [DataRow(15)]
        public void EnsureValidLifetime_WithinTheAllowedRange_DoesNotThrow(int minutes)
        {
            var act = () => Options(minutes).EnsureValidLifetime();

            act.Should().NotThrow();
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(-5)]
        [DataRow(16)]
        [DataRow(60)]
        public void EnsureValidLifetime_OutsideTheAllowedRange_ThrowsWithTheLimits(int minutes)
        {
            var act = () => Options(minutes).EnsureValidLifetime();

            act.Should().Throw<InvalidOperationException>().WithMessage("*TokenExpiresInMinutes*1*15*");
        }
    }
}