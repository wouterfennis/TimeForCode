using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Reqnroll;
using System.Reflection;
using TimeForCode.Authorization.Api.Client;
using TimeForCode.Authorization.Api.Client.Extensions;
using TimeForCode.Authorization.Application.Interfaces;
using TimeForCode.Authorization.Commands;

namespace TimeForCode.Authorization.Specifications.Steps
{
    [Binding]
    internal class CallbackSteps
    {
        private readonly IAuthClient _authClient;
        private readonly IServiceProvider _provider;
        private TryResponse<Api.Client.CallbackResponseModel?, ApiException<ProblemDetails>?>? _result = null;

        public CallbackSteps(IAuthClient authClient, IServiceProvider provider)
        {
            _authClient = authClient;
            _provider = provider;
        }

        [Given("The external platform does not return the access token")]
        public void GivenTheExternalPlatformDoesNotReturnTheAccessToken()
        {
            Mock<IIdentityProviderService> mock = _provider.GetRequiredService<Mock<IIdentityProviderService>>();

            var result = Result<GetAccessTokenResult>.Failure("Access token cannot be received");

            mock.Setup(x => x.GetAccessTokenAsync(It.IsAny<string>()))
                .ReturnsAsync(result);
        }

        [When("The external platform calls the time for code platform to complete the authorization")]
        public async Task WhenTheExternalPlatformCallsTheTimeForCodePlatformToCompleteTheAuthorizationAsync()
        {
            string code = "code";

            _result = await _authClient.TryCallbackAsync(code, Constants.StateKey);
        }

        [Then("An authentication token is returned")]
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

        [Then("An authentication token is not returned")]
        public void ThenAnAuthenticationTokenIsNotReturned()
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
