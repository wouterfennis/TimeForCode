using FluentAssertions;
using Microsoft.AspNetCore.Http;
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

        [Given("The user logs in at the time for code platform")]
        [When("The user logs in at the time for code platform")]
        public async Task WhenTheUserLogsInAtTheTimeForCodePlatformAsync()
        {
            _result = await _authClient.TryLoginAsync(IdentityProvider.Github, new Uri("http://localhost:8082"));
        }

        [When("The user logs in at the time for code platform with an invalid redirect url")]
        public async Task WhenTheUserLogsInAtTheTimeForCodePlatformWithAnInvalidRedirectUrlAsync()
        {
            _result = await _authClient.TryLoginAsync(IdentityProvider.Github, new Uri("http://invalid-uri.com"));
        }

        [Then("The user is redirected to the external platform")]
        public void ThenTheUserIsRedirectedToTheExternalPlatform()
        {
            _result.Should().NotBeNull();
            _result!.Exception.Should().NotBeNull();
            _result!.Exception!.Headers["Location"].Single().Should().NotBeNullOrEmpty();
        }

        [Then("The user is informed the login redirect uri is rejected")]
        public void ThenTheUserIsInformedTheRedirectUriIsRejected()
        {
            _result.Should().NotBeNull();
            _result!.Exception.Should().NotBeNull();
            _result!.Exception!.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        }
    }
}