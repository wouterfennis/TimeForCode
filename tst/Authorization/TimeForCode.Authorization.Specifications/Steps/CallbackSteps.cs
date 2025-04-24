using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Reqnroll;
using RichardSzalay.MockHttp;
using System.Net;
using System.Text.Json;
using TimeForCode.Authorization.Api.Client;
using TimeForCode.Authorization.Api.Client.Extensions;
using TimeForCode.Authorization.Application.Interfaces;
using TimeForCode.Authorization.Domain.Entities;
using TimeForCode.Authorization.Values;

namespace TimeForCode.Authorization.Specifications.Steps
{
    [Binding]
    internal class CallbackSteps
    {
        private readonly IAuthClient _authClient;
        private readonly IServiceProvider _provider;
        private readonly CookieContainer _cookieContainer;
        private TryResponse<CallbackResponseModel?, Exception?>? _result = null;

        public CallbackSteps(IAuthClient authClient, IServiceProvider provider, CookieContainer cookieContainer)
        {
            _authClient = authClient;
            _provider = provider;
            _cookieContainer = cookieContainer;
        }

        [Given("The external platform does not return the access token")]
        public void GivenTheExternalPlatformDoesNotReturnTheAccessToken()
        {
            var mockHttp = _provider.GetRequiredService<MockHttpMessageHandler>();

            var problemDetails = new Microsoft.AspNetCore.Mvc.ProblemDetails
            {
                Title = "Access token cannot be received",
                Status = (int)HttpStatusCode.NotFound,
                Detail = "Access token cannot be received"
            };

            mockHttp.When("https://localhost:8081/login/oauth/access_token")
                .Respond(HttpStatusCode.NotFound, "application/json", JsonSerializer.Serialize(problemDetails));
        }

        [Given("The external platform calls the time for code platform to complete the authorization")]
        [When("The external platform calls the time for code platform to complete the authorization")]
        public async Task WhenTheExternalPlatformCallsTheTimeForCodePlatformToCompleteTheAuthorizationAsync()
        {
            string code = "code";

            _result = await _authClient.TryCallbackAsync(code, Constants.StateKey);
        }

        [Then("An access token is returned")]
        public void ThenAAccessTokenIsReturned()
        {
            var accessTokenCookie = _cookieContainer.GetAllCookies()[CookieConstants.TokenKey];
            accessTokenCookie.Should().NotBeNull();
        }

        [Then("The user information is saved in the time for code platform")]
        public void ThenTheUserInformationIsSavedInTheTimeForCodePlatform()
        {
            var repository = _provider.GetRequiredService<Mock<IAccountInformationRepository>>();

            repository.Verify(r => r.CreateOrUpdateAsync(It.IsAny<AccountInformation>()));
        }

        [Then("An access token is not returned")]
        public void ThenAnAccessTokenIsNotReturned()
        {
            _result.Should().NotBeNull();
        }

        [Then("The following callback error message is returned: {string}")]
        public void ThenTheFollowingCallbackErrorMessageIsReturned(string errorMessage)
        {
            _result!.Exception.Should().NotBeNull();
            _result!.Exception.Should().BeOfType<ApiException<ProblemDetails>>();

            var exception = _result!.Exception as ApiException<ProblemDetails>;

            var problemDetails = exception!.Result;
            problemDetails!.Detail.Should().NotBeNull();
            problemDetails.Detail.Should().Contain(errorMessage);
        }
    }
}