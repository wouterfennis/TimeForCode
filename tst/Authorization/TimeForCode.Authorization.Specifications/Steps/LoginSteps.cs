using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Reqnroll;
using TimeForCode.Authorization.Api.Client;
using TimeForCode.Authorization.Api.Client.Extensions;
using TimeForCode.Authorization.Specifications.Mocking;
using TimeForCode.Authorization.Values;

namespace TimeForCode.Authorization.Specifications.Steps
{
    [Binding]
    internal partial class LoginSteps
    {
        private readonly IAuthClient _authClient;
        private TryVoid<ApiException?>? _result = null;
        private readonly ScenarioInfrastructure _infrastructure;

        public LoginSteps(IAuthClient authClient, ScenarioInfrastructure infrastructure)
        {
            _authClient = authClient;
            _infrastructure = infrastructure;
        }

        [Given("The user logs in at the time for code platform")]
        [Given("The user logs in at one instance of the time for code platform")]
        [When("The user logs in at the time for code platform")]
        public async Task WhenTheUserLogsInAtTheTimeForCodePlatformAsync()
        {
            _result = await _authClient.TryLoginAsync(IdentityProvider.Github, new Uri("http://localhost:8084"));
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

        [Then("The redirect does not request profile write or email access")]
        public void ThenTheRedirectDoesNotRequestProfileWriteOrEmailAccess()
        {
            var location = new Uri(_result!.Exception!.Headers["Location"].Single());

            System.Web.HttpUtility.ParseQueryString(location.Query).AllKeys.Should().NotContain("scope");
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