using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Moq;
using Reqnroll;
using RichardSzalay.MockHttp;
using System.Net;
using TimeForCode.Authorization.Api.Client;
using TimeForCode.Authorization.Api.Client.Extensions;
using TimeForCode.Authorization.Domain;

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
            var mockHttp = _provider.GetRequiredService<MockHttpMessageHandler>();

            mockHttp.When("https://github.com/login/oauth/access_token")
                .Respond(HttpStatusCode.NotFound, "application/json", "Access token cannot be received");
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
            var mongoCollection = _provider.GetRequiredService<Mock<IMongoCollection<AccountInformation>>>();

            mongoCollection.Verify(mongoCollection => mongoCollection.InsertOneAsync(It.IsAny<AccountInformation>(), It.IsAny<InsertOneOptions>(), It.IsAny<CancellationToken>()), Times.Once);
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