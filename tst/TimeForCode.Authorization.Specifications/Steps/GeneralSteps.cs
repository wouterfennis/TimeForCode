using Microsoft.Extensions.DependencyInjection;
using Moq;
using Reqnroll;
using Reqnroll.BoDi;
using TimeForCode.Authorization.Application.Interfaces;
using TimeForCode.Authorization.Commands;

namespace TimeForCode.Authorization.Specifications.Steps
{
    [Binding]
    internal class GeneralSteps
    {
        private readonly IServiceProvider _provider;

        public GeneralSteps(IServiceProvider provider)
        {
            _provider = provider;
        }

        [Given("The user has an account at the external platform")]
        public void GivenTheUserHasAnAccountAtTheExternalPlatform()
        {
            Mock<IIdentityProviderService> mock = _provider.GetRequiredService<Mock<IIdentityProviderService>>();

            var accountInformation = new AccountInformation
            {
                Id = 1,
                Name = "John Doe",
                Email = "",
                AvatarUrl = "https://avatars.githubusercontent.com/u/1?v=4",
                Login = "johndoe",
                NodeId = "",
                Company = "",
            };
            var result = Result<AccountInformation>.Success(accountInformation);

            mock
                .Setup(x => x.GetAccountInformation(It.IsAny<GetAccountInformationModel>()))
                .ReturnsAsync(result);
        }

        [Given("The user logs in at the external platform")]
        public void GivenTheUserLogsInAtTheExternalPlatform()
        {
            Mock<IIdentityProviderService> mock = _provider.GetRequiredService<Mock<IIdentityProviderService>>();

            var accessTokenResult = new GetAccessTokenResult
            {
                AccessToken = "token",
                Scope = "scope",
                TokenType = "token_type"
            };
            var result = Result<GetAccessTokenResult>.Success(accessTokenResult);

            mock
                .Setup(x => x.GetAccessTokenAsync(It.IsAny<string>()))
                .ReturnsAsync(result);
        }
    }
}
