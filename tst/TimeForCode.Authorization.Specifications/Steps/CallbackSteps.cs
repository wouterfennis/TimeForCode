using FluentAssertions;
using Reqnroll;
using TimeForCode.Authorization.Api.Client;

namespace TimeForCode.Authorization.Specifications.Steps
{
    [Binding]
    internal class CallbackSteps
    {
        private readonly IAuthClient _authClient;
        private object? _result = null;

        public CallbackSteps(IAuthClient authClient)
        {
            _authClient = authClient;
        }

        [When("The external platform calls the time for code platform to complete the authorization")]
        public async Task WhenTheExternalPlatformCallsTheTimeForCodePlatformToCompleteTheAuthorizationAsync()
        {
            string code = "code";
            string state = "state";

            _result = await _authClient.CallbackAsync(code, state);
        }

        [Then("A authentication token is returned")]
        public void ThenAAuthenticationTokenIsReturned()
        {
            _result.Should().NotBeNull();
            _result.Should().BeOfType<OkObjectResult>();
            var ok_result = _result as OkObjectResult;
            ok_result!.Value.Should().NotBeNull();
            ok_result.Value.Should().BeOfType<string>();
        }


        [Then("The user information is saved in the time for code platform")]
        public void ThenTheUserInformationIsSavedInTheTimeForCodePlatform()
        {
           // throw new PendingStepException();
        }
    }
}
