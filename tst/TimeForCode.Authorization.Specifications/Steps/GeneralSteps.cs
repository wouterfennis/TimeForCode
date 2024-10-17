using Microsoft.Extensions.DependencyInjection;
using Reqnroll;
using RichardSzalay.MockHttp;
using System.Net;
using System.Text.Json;
using TimeForCode.Authorization.Application.Interfaces;
using TimeForCode.Authorization.Domain;

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
            var mockHttp = _provider.GetRequiredService<MockHttpMessageHandler>();

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

            mockHttp.When("https://api.github.com/user")
                    .Respond("application/json", JsonSerializer.Serialize(accountInformation));
        }

        [Given("The user has no account at the external platform")]
        public void GivenTheUserHasNoAccountAtTheExternalPlatform()
        {
            var mockHttp = _provider.GetRequiredService<MockHttpMessageHandler>();

            var result = Commands.Result<AccountInformation>.Failure("No account information");

            mockHttp.When("https://api.github.com/user")
                    .Respond(HttpStatusCode.NotFound, "application/json", "No account information");
        }

        [Given("The user logs in at the external platform")]
        public void GivenTheUserLogsInAtTheExternalPlatform()
        {
            var mockHttp = _provider.GetRequiredService<MockHttpMessageHandler>();

            var accessTokenResult = new GetAccessTokenResult
            {
                AccessToken = "token",
                Scope = "scope",
                TokenType = "token_type"
            };

            mockHttp.When("https://github.com/login/oauth/access_token")
                .Respond("application/json", JsonSerializer.Serialize(accessTokenResult));
        }

        [Given("The user has not logged in at the external platform")]
        public void GivenTheUserHasNotLoggedInAtTheExternalPlatform()
        {
            // No need to do anything here
        }

    }
}