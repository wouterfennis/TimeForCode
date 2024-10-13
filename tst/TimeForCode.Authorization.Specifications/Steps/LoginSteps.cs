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
        private TryVoid<ApiException?>? _result = null;

        public LoginSteps(IAuthClient authClient)
        {
            _authClient = authClient;
        }

        [When("The user logs in at the time for code platform")]
        public async Task WhenTheUserLogsInAtTheTimeForCodePlatformAsync()
        {
            var model = new LoginRequestModel
            {
                IdentityProvider = IdentityProvider.Github
            };

            _result = await _authClient.TryLoginAsync(model);
        }

        [Then("The user is redirected to the external platform")]
        public void ThenTheUserIsRedirectedToTheExternalPlatform()
        {
            _result.Should().NotBeNull();
            _result!.Exception.Should().NotBeNull();
            _result!.Exception!.Headers["Location"].Single().Should().NotBeNullOrEmpty();
        }
    }
}
