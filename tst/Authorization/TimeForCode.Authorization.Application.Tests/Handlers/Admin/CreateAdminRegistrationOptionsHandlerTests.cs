using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using TimeForCode.Authorization.Application.Handlers.Admin;
using TimeForCode.Authorization.Application.Interfaces.Admin;
using TimeForCode.Authorization.Application.Options;
using TimeForCode.Authorization.Commands.Admin;
using TimeForCode.Authorization.Domain.Entities;

namespace TimeForCode.Authorization.Application.Tests.Handlers.Admin
{
    [TestClass]
    public class CreateAdminRegistrationOptionsHandlerTests
    {
        private Mock<IAdminCredentialRepository> _repositoryMock = null!;
        private Mock<IPasskeyCeremonyService> _ceremonyServiceMock = null!;
        private CreateAdminRegistrationOptionsHandler _sut = null!;

        [TestInitialize]
        public void Setup()
        {
            _repositoryMock = new Mock<IAdminCredentialRepository>();
            _ceremonyServiceMock = new Mock<IPasskeyCeremonyService>();

            var options = Microsoft.Extensions.Options.Options.Create(new AdminPasskeyOptions
            {
                ServerDomain = "localhost",
                RelyingPartyName = "TimeForCode Admin",
                BootstrapSecret = "correct-secret"
            });

            _sut = new CreateAdminRegistrationOptionsHandler(
                _repositoryMock.Object,
                _ceremonyServiceMock.Object,
                options,
                NullLogger<CreateAdminRegistrationOptionsHandler>.Instance);
        }

        [TestMethod]
        public async Task Handle_CorrectSecretAndNoExistingCredential_ReturnsSuccess()
        {
            _repositoryMock.Setup(x => x.GetAsync()).ReturnsAsync((AdminCredential?)null);
            _ceremonyServiceMock.Setup(x => x.CreateRegistrationOptionsAsync())
                .ReturnsAsync(new PasskeyCeremonyOptionsResult { OptionsJson = "{}", ProtectedCeremonyState = "state" });

            var result = await _sut.Handle(new CreateAdminRegistrationOptionsCommand { BootstrapSecret = "correct-secret" }, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.OptionsJson.Should().Be("{}");
        }

        [TestMethod]
        public async Task Handle_IncorrectSecret_ReturnsFailure()
        {
            var result = await _sut.Handle(new CreateAdminRegistrationOptionsCommand { BootstrapSecret = "wrong-secret" }, CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            _ceremonyServiceMock.Verify(x => x.CreateRegistrationOptionsAsync(), Times.Never);
        }

        [TestMethod]
        public async Task Handle_CredentialAlreadyClaimed_ReturnsFailure()
        {
            _repositoryMock.Setup(x => x.GetAsync()).ReturnsAsync(new AdminCredential
            {
                Id = AdminCredential.SingletonId,
                CredentialId = [1],
                PublicKey = [2],
                SignCount = 0,
                Transports = [],
                IsUserVerified = true,
                IsBackupEligible = false,
                IsBackedUp = false,
                CreatedAt = DateTimeOffset.UtcNow
            });

            var result = await _sut.Handle(new CreateAdminRegistrationOptionsCommand { BootstrapSecret = "correct-secret" }, CancellationToken.None);

            result.IsFailure.Should().BeTrue();
        }
    }
}