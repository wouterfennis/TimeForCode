using FluentAssertions;
using Reqnroll;
using TimeForCode.Authorization.Api.Client;
using TimeForCode.Authorization.Api.Client.Extensions;

namespace TimeForCode.Authorization.Specifications.Steps
{
    [Binding]
    internal class LoginSteps
    {
        private readonly IAuthClient _authClient;
        private RedirectResult? _result = null;

        public LoginSteps(IAuthClient authClient)
        {
            _authClient = authClient;
        }

        [When("The user logs in at the time for code platform")]
        public async Task WhenTheUserLogsInAtTheTimeForCodePlatformAsync()
        {
            var model = new LoginModel
            {
                IdentityProvider = IdentityProvider.Github
            };

            _result = await _authClient.LoginWithRedirectAsync(model);
        }

        [Then("The user is redirected to the external platform")]
        public void ThenTheUserIsRedirectedToTheExternalPlatform()
        {
            _result.Should().NotBeNull();
            _result!.Url.Should().NotBeNullOrEmpty();
        }
    }
}
