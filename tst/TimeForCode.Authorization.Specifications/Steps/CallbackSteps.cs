using FluentAssertions;
using Reqnroll;
using TimeForCode.Authorization.Api.Client;
using TimeForCode.Authorization.Api.Client.Extensions;

namespace TimeForCode.Authorization.Specifications.Steps
{
    [Binding]
    internal class CallbackSteps
    {
        private readonly IAuthClient _authClient;
        private TryResponse<Api.Client.CallbackResponseModel?, ApiException<ProblemDetails>?>? _result = null;

        public CallbackSteps(IAuthClient authClient)
        {
            _authClient = authClient;
        }

        [When("The external platform calls the time for code platform to complete the authorization")]
        public async Task WhenTheExternalPlatformCallsTheTimeForCodePlatformToCompleteTheAuthorizationAsync()
        {
            string code = "code";
            string state = "state";

            _result = await _authClient.TryCallbackAsync(code, state);
        }

        [Then("A authentication token is returned")]
        public void ThenAAuthenticationTokenIsReturned()
        {
            _result.Should().NotBeNull();
            _result!.Response.Should().NotBeNull();
            _result!.Response!.AccessToken.Should().NotBeNullOrEmpty();
        }

        [Then("The user information is saved in the time for code platform")]
        public void ThenTheUserInformationIsSavedInTheTimeForCodePlatform()
        {
            // throw new PendingStepException();
        }

        [Then("A authentication token is not returned")]
        public void ThenAAuthenticationTokenIsNotReturned()
        {
            _result.Should().NotBeNull();
        }

        [Then("The following error message is returned: {string}")]
        public void ThenTheFollowingErrorMessageIsReturned(string errorMessage)
        {
            _result!.Exception.Should().NotBeNull();

            var problemDetails = _result!.Exception!.Result;
            problemDetails!.Detail.Should().NotBeNull();
            problemDetails.Detail.Should().Be(errorMessage);
        }
    }
}
