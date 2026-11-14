using AwesomeAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Moq;
using TimeForCode.Authorization.Application.Interfaces;
using TimeForCode.Authorization.Application.Options;
using TimeForCode.Authorization.Domain.Entities;
using TimeForCode.Authorization.Infrastructure.Services.Admin;

namespace TimeForCode.Authorization.Infrastructure.Tests.Services
{
    [TestClass]
    public class PasskeyCeremonyServiceTests
    {
        private Mock<IPasskeyHandler<AdminPasskeyUser>> _passkeyHandler = null!;
        private InMemoryCeremonyStateRepository _repository = null!;
        private CeremonyStateStore _states = null!;
        private PasskeyCeremonyService _sut = null!;

        [TestInitialize]
        public void Setup()
        {
            _passkeyHandler = new Mock<IPasskeyHandler<AdminPasskeyUser>>();
            _passkeyHandler.Setup(x => x.PerformAssertionAsync(It.IsAny<PasskeyAssertionContext>()))
                .ReturnsAsync(PasskeyAssertionResult.Fail<AdminPasskeyUser>(new PasskeyException("rejected by the test")));
            var encryptionService = new Mock<IEncryptionService>();
            encryptionService.Setup(x => x.Decrypt(It.IsAny<string>())).Returns<string>(value => value);
            encryptionService.Setup(x => x.Encrypt(It.IsAny<string>())).Returns<string>(value => value);

            var httpContextAccessor = new Mock<IHttpContextAccessor>();
            httpContextAccessor.Setup(x => x.HttpContext).Returns(new DefaultHttpContext());
            var options = Microsoft.Extensions.Options.Options.Create(new AdminPasskeyOptions
            {
                ServerDomain = "localhost",
                RelyingPartyName = "TimeForCode",
                BootstrapSecret = "secret"
            });
            _repository = new InMemoryCeremonyStateRepository();
            _states = new CeremonyStateStore(_repository, TimeProvider.System, options);

            _sut = new PasskeyCeremonyService(_passkeyHandler.Object, httpContextAccessor.Object, encryptionService.Object, _states, options);
        }

        [TestMethod]
        public async Task CompleteAuthenticationAsync_UnknownCeremony_ReturnsFailedOutcomeWithoutCallingThePasskeyLibrary()
        {
            var outcome = await _sut.CompleteAuthenticationAsync("credential", "never-issued");

            outcome.Succeeded.Should().BeFalse();
            _passkeyHandler.Verify(x => x.PerformAssertionAsync(It.IsAny<PasskeyAssertionContext>()), Times.Never);
        }

        [TestMethod]
        public async Task CompleteRegistrationAsync_UnknownCeremony_ReturnsFailedOutcomeWithoutCallingThePasskeyLibrary()
        {
            var outcome = await _sut.CompleteRegistrationAsync("credential", "never-issued");

            outcome.Succeeded.Should().BeFalse();
            _passkeyHandler.Verify(x => x.PerformAttestationAsync(It.IsAny<PasskeyAttestationContext>()), Times.Never);
        }

        [TestMethod]
        public async Task CompleteAuthenticationAsync_CeremonyAlreadyUsed_ReturnsFailedOutcome()
        {
            var id = await _states.StartAsync(CeremonyKind.Authentication, "state");
            await _sut.CompleteAuthenticationAsync("credential", id);
            _passkeyHandler.Invocations.Clear();

            var replay = await _sut.CompleteAuthenticationAsync("credential", id);

            replay.Succeeded.Should().BeFalse("the same ceremony id and assertion cannot be replayed");
            _passkeyHandler.Verify(x => x.PerformAssertionAsync(It.IsAny<PasskeyAssertionContext>()), Times.Never);
        }

        [TestMethod]
        public async Task CompleteAuthenticationAsync_RegistrationCeremony_ReturnsFailedOutcome()
        {
            var id = await _states.StartAsync(CeremonyKind.Registration, "state");

            var outcome = await _sut.CompleteAuthenticationAsync("credential", id);

            outcome.Succeeded.Should().BeFalse();
            _passkeyHandler.Verify(x => x.PerformAssertionAsync(It.IsAny<PasskeyAssertionContext>()), Times.Never);
        }

        [TestMethod]
        public async Task CompleteAuthenticationAsync_ValidCeremony_PassesTheStoredStateToThePasskeyLibraryOnce()
        {
            var id = await _states.StartAsync(CeremonyKind.Authentication, "the-state");

            await _sut.CompleteAuthenticationAsync("credential", id);

            _passkeyHandler.Verify(x => x.PerformAssertionAsync(It.Is<PasskeyAssertionContext>(c => c.AssertionState == "the-state" && c.CredentialJson == "credential")), Times.Once);
            _repository.Entries.Should().BeEmpty();
        }
    }
}