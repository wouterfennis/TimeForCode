using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TimeForCode.Authorization.Application.Handlers.Admin;
using TimeForCode.Authorization.Application.Interfaces.Admin;
using TimeForCode.Authorization.Commands.Admin;
using TimeForCode.Authorization.Domain.Entities;

namespace TimeForCode.Authorization.Application.Tests.Handlers.Admin
{
    [TestClass]
    public class CreateAdminAuthenticationOptionsHandlerTests
    {
        private Mock<IAdminCredentialRepository> _repositoryMock = null!;
        private Mock<IPasskeyCeremonyService> _ceremonyServiceMock = null!;
        private CreateAdminAuthenticationOptionsHandler _sut = null!;

        [TestInitialize]
        public void Setup()
        {
            _repositoryMock = new Mock<IAdminCredentialRepository>();
            _ceremonyServiceMock = new Mock<IPasskeyCeremonyService>();

            _sut = new CreateAdminAuthenticationOptionsHandler(
                _repositoryMock.Object,
                _ceremonyServiceMock.Object,
                NullLogger<CreateAdminAuthenticationOptionsHandler>.Instance);
        }

        [TestMethod]
        public async Task Handle_CredentialRegistered_ReturnsSuccess()
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
            _ceremonyServiceMock.Setup(x => x.CreateAuthenticationOptionsAsync())
                .ReturnsAsync(new PasskeyCeremonyOptionsResult { OptionsJson = "{}", ProtectedCeremonyState = "state" });

            var result = await _sut.Handle(new CreateAdminAuthenticationOptionsCommand(), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
        }

        [TestMethod]
        public async Task Handle_NoCredentialRegistered_ReturnsFailure()
        {
            _repositoryMock.Setup(x => x.GetAsync()).ReturnsAsync((AdminCredential?)null);

            var result = await _sut.Handle(new CreateAdminAuthenticationOptionsCommand(), CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            _ceremonyServiceMock.Verify(x => x.CreateAuthenticationOptionsAsync(), Times.Never);
        }
    }
}