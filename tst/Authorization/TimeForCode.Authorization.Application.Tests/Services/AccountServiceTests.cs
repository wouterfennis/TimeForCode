using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using Moq;
using TimeForCode.Authorization.Application.Interfaces;
using TimeForCode.Authorization.Application.Services;
using TimeForCode.Authorization.Commands;
using TimeForCode.Authorization.Domain.Entities;
using TimeForCode.Authorization.Values;

namespace TimeForCode.Authorization.Application.Tests.Services
{
    [TestClass]
    public class AccountServiceTests
    {
        private Mock<IIdentityProviderServiceFactory> _factoryMock = null!;
        private Mock<IIdentityProviderService> _providerMock = null!;
        private Mock<IAccountInformationRepository> _repositoryMock = null!;
        private AccountService _sut = null!;

        [TestInitialize]
        public void Setup()
        {
            _factoryMock = new Mock<IIdentityProviderServiceFactory>();
            _providerMock = new Mock<IIdentityProviderService>();
            _repositoryMock = new Mock<IAccountInformationRepository>();
            _factoryMock.Setup(x => x.GetIdentityProviderService(IdentityProvider.Github))
                .Returns(Result<IIdentityProviderService>.Success(_providerMock.Object));

            _sut = new AccountService(_factoryMock.Object, _repositoryMock.Object, NullLogger<AccountService>.Instance);
        }

        [TestMethod]
        public async Task SaveAccountInformation_ValidToken_SavesProfileWithoutTheExternalToken()
        {
            var account = BuildAccount();
            _providerMock.Setup(x => x.GetAccountInformation(It.IsAny<ExternalAccessToken>()))
                .ReturnsAsync(Result<AccountInformation>.Success(account));
            _repositoryMock.Setup(x => x.CreateOrUpdateAsync(account))
                .ReturnsAsync(new CreateOrUpdateResult(account, IsNewAccount: true));

            var result = await _sut.SaveAccountInformation(IdentityProvider.Github, new ExternalAccessToken { Token = "external-secret" });

            result.IsSuccess.Should().BeTrue();
            _repositoryMock.Verify(x => x.CreateOrUpdateAsync(It.Is<AccountInformation>(a =>
                !System.Text.Json.JsonSerializer.Serialize(a).Contains("external-secret"))));
        }

        [TestMethod]
        public async Task SaveAccountInformation_ProviderFails_ReturnsFailureAndSavesNothing()
        {
            _providerMock.Setup(x => x.GetAccountInformation(It.IsAny<ExternalAccessToken>()))
                .ReturnsAsync(Result<AccountInformation>.Failure("nope"));

            var result = await _sut.SaveAccountInformation(IdentityProvider.Github, new ExternalAccessToken { Token = "t" });

            result.IsFailure.Should().BeTrue();
            _repositoryMock.Verify(x => x.CreateOrUpdateAsync(It.IsAny<AccountInformation>()), Times.Never);
        }

        private static AccountInformation BuildAccount() => new()
        {
            Id = ObjectId.GenerateNewId(),
            IdentityProviderId = "42",
            Login = "octocat",
            NodeId = "node",
            AvatarUrl = "https://avatars.githubusercontent.com/u/42",
            Name = "Octo Cat",
            Company = null,
            Email = "octo@example.com",
            Bio = null,
            Location = null
        };
    }
}