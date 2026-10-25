using AwesomeAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Moq;
using System.Security.Cryptography;
using TimeForCode.Authorization.Application.Interfaces;
using TimeForCode.Authorization.Application.Options;
using TimeForCode.Authorization.Infrastructure.Services.Admin;

namespace TimeForCode.Authorization.Infrastructure.Tests.Services
{
    [TestClass]
    public class PasskeyCeremonyServiceTests
    {
        private Mock<IPasskeyHandler<AdminPasskeyUser>> _passkeyHandler = null!;
        private PasskeyCeremonyService _sut = null!;

        [TestInitialize]
        public void Setup()
        {
            _passkeyHandler = new Mock<IPasskeyHandler<AdminPasskeyUser>>();
            var encryptionService = new Mock<IEncryptionService>();
            encryptionService.Setup(x => x.Decrypt(It.IsAny<string>())).Throws<CryptographicException>();

            var httpContextAccessor = new Mock<IHttpContextAccessor>();
            httpContextAccessor.Setup(x => x.HttpContext).Returns(new DefaultHttpContext());
            var options = Microsoft.Extensions.Options.Options.Create(new AdminPasskeyOptions
            {
                ServerDomain = "localhost",
                RelyingPartyName = "TimeForCode",
                BootstrapSecret = "secret"
            });

            _sut = new PasskeyCeremonyService(_passkeyHandler.Object, httpContextAccessor.Object, encryptionService.Object, options);
        }

        [TestMethod]
        public async Task CompleteAuthenticationAsync_UnreadableCeremonyState_ReturnsFailedOutcome()
        {
            var outcome = await _sut.CompleteAuthenticationAsync("credential", "garbage");

            outcome.Succeeded.Should().BeFalse();
            _passkeyHandler.Verify(x => x.PerformAssertionAsync(It.IsAny<PasskeyAssertionContext>()), Times.Never);
        }

        [TestMethod]
        public async Task CompleteRegistrationAsync_UnreadableCeremonyState_ReturnsFailedOutcome()
        {
            var outcome = await _sut.CompleteRegistrationAsync("credential", "garbage");

            outcome.Succeeded.Should().BeFalse();
            _passkeyHandler.Verify(x => x.PerformAttestationAsync(It.IsAny<PasskeyAttestationContext>()), Times.Never);
        }
    }
}