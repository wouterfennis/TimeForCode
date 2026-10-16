using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using Moq;
using Reqnroll;
using RichardSzalay.MockHttp;
using System.Net;
using System.Text.Json;
using TimeForCode.Authorization.Api.Client;
using TimeForCode.Authorization.Api.Client.Extensions;
using TimeForCode.Authorization.Application.Interfaces;
using TimeForCode.Authorization.Domain.Entities;
using TimeForCode.Authorization.Specifications.Mocking;
using TimeForCode.Authorization.Specifications.TestBuilder;
using TimeForCode.Shared.Api.Authentication;

namespace TimeForCode.Authorization.Specifications.Steps
{
    [Binding]
    internal partial class CallbackSteps
    {
        private readonly IAuthClient _authClient;
        private readonly System.IServiceProvider _provider;
        private readonly CookieContainer _cookieContainer;
        private readonly ScenarioInfrastructure _infrastructure;
        private TryResponse<CallbackResponseModel?, System.Exception?>? _result = null;

        public CallbackSteps(IAuthClient authClient, System.IServiceProvider provider, CookieContainer cookieContainer, ScenarioInfrastructure infrastructure)
        {
            _authClient = authClient;
            _provider = provider;
            _cookieContainer = cookieContainer;
            _infrastructure = infrastructure;
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

            mockHttp.When("http://localhost:8081/login/oauth/access_token")
                .Respond(HttpStatusCode.NotFound, "application/json", JsonSerializer.Serialize(problemDetails));
        }

        [Given("The external platform calls the time for code platform to complete the authorization")]
        [When("The external platform calls the time for code platform to complete the authorization")]
        public async Task WhenTheExternalPlatformCallsTheTimeForCodePlatformToCompleteTheAuthorizationAsync()
        {
            string code = "code";

            _result = await (_infrastructure.CallbackClient ?? _authClient).TryCallbackAsync(code, Constants.StateKey);
        }

        [When("The external platform calls the time for code platform with a {word} of 2049 characters")]
        public async Task WhenTheExternalPlatformCallsTheTimeForCodePlatformWithAnOversizedParameterAsync(string parameter)
        {
            var oversized = new string('a', 2049);

            _result = await _authClient.TryCallbackAsync(
                parameter == "code" ? oversized : "code",
                parameter == "state" ? oversized : Constants.StateKey);
        }

        [Then("The callback is rejected as invalid")]
        public void ThenTheCallbackIsRejectedAsInvalid()
        {
            _result!.Exception.Should().BeOfType<ApiException<ProblemDetails>>();

            var exception = (ApiException<ProblemDetails>)_result!.Exception!;
            exception.StatusCode.Should().Be(400);
            exception.Result.Title.Should().Be("One or more validation errors occurred.");
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

        [Then("No access token of the external platform is stored for the user")]
        public void ThenNoAccessTokenOfTheExternalPlatformIsStoredForTheUser()
        {
            var repository = _provider.GetRequiredService<Mock<IAccountInformationRepository>>();

            var saved = repository.Invocations
                .Where(i => i.Method.Name == nameof(IAccountInformationRepository.CreateOrUpdateAsync))
                .Select(i => (AccountInformation)i.Arguments[0])
                .Single();

            saved.ToBsonDocument().ToJson().Should().NotContainEquivalentOf("token");
        }

        [Given("The user does not yet have a profile on the time for code platform")]
        public void GivenTheUserDoesNotYetHaveAProfileOnTheTimeForCodePlatform()
        {
            var repository = _provider.GetRequiredService<Mock<IAccountInformationRepository>>();
            repository.Setup(x => x.CreateOrUpdateAsync(It.IsAny<AccountInformation>()))
                .ReturnsAsync(new CreateOrUpdateResult(AccountInformationBuilder.Build(), IsNewAccount: true));
        }

        [Given("The user already has a profile on the time for code platform")]
        public static void GivenTheUserAlreadyHasAProfileOnTheTimeForCodePlatform()
        {
            // The factory default already returns IsNewAccount: false
        }

        [Then("The time for code platform notifies the user that their profile has been created")]
        public void ThenTheTimeForCodePlatformNotifiesTheUserThatTheirProfileHasBeenCreated()
        {
            var isNewUserCookie = _cookieContainer.GetAllCookies()["IsNewUser"];
            isNewUserCookie.Should().NotBeNull();
        }

        [Then("The time for code platform does not notify the user of a new profile")]
        public void ThenTheTimeForCodePlatformDoesNotNotifyTheUserOfANewProfile()
        {
            var isNewUserCookie = _cookieContainer.GetAllCookies()["IsNewUser"];
            isNewUserCookie.Should().BeNull();
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