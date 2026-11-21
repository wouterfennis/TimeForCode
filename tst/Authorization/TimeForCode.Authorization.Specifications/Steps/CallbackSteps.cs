using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using Moq;
using Reqnroll;
using RichardSzalay.MockHttp;
using System.Net;
using System.Text.Json;
using System.Web;
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
        private TryVoid<ApiException?>? _result = null;
        private readonly SessionContext _session;

        public CallbackSteps(IAuthClient authClient, System.IServiceProvider provider, CookieContainer cookieContainer, ScenarioInfrastructure infrastructure, SessionContext session)
        {
            _authClient = authClient;
            _provider = provider;
            _cookieContainer = cookieContainer;
            _infrastructure = infrastructure;
            _session = session;
        }

        [Given("The external platform does not return the access token")]
        public void GivenTheExternalPlatformDoesNotReturnTheAccessToken()
        {
            ExternalPlatformMocks.RegisterNotFound(_provider.GetRequiredService<MockHttpMessageHandler>(), "/login/oauth/access_token", "Access token cannot be received");
        }

        [Given("The external platform calls the time for code platform to complete the authorization")]
        [When("The external platform calls the time for code platform to complete the authorization")]
        public async Task WhenTheExternalPlatformCallsTheTimeForCodePlatformToCompleteTheAuthorizationAsync()
        {
            string code = "code";

            _session.OneTimeCode = null;
            _result = await (_infrastructure.CallbackClient ?? _authClient).TryCallbackAsync(code, Constants.StateKey);
            RememberTheOneTimeCode();
        }

        private void RememberTheOneTimeCode()
        {
            // A completed sign-in is a redirect (the generated client reports it as an exception with status 302) carrying the code.
            if (_result!.Exception is { StatusCode: 302 } redirect && redirect.Headers.TryGetValue("Location", out var location))
            {
                _session.OneTimeCode = HttpUtility.ParseQueryString(new Uri(location.Single()).Query)["code"];
            }
        }

        [When("The external platform calls the time for code platform with a {word} of 2049 characters")]
        public async Task WhenTheExternalPlatformCallsTheTimeForCodePlatformWithAnOversizedParameterAsync(string parameter)
        {
            var oversized = new string('a', 2049);

            _result = await _authClient.TryCallbackAsync(
                parameter == "code" ? oversized : "code",
                parameter == "state" ? oversized : Constants.StateKey);
        }

        [Then("The user is redirected back with a one-time code")]
        public void ThenTheUserIsRedirectedBackWithAOneTimeCode()
        {
            _result!.Exception.Should().NotBeNull();
            _result.Exception!.StatusCode.Should().Be(302);
            _session.OneTimeCode.Should().NotBeNullOrEmpty();
        }

        [Then("The one-time code is not a token")]
        public void ThenTheOneTimeCodeIsNotAToken()
        {
            var location = new Uri(_result!.Exception!.Headers["Location"].Single());

            location.Query.Should().NotContainEquivalentOf("token");
            _session.OneTimeCode.Should().NotContain(".", "a JWT has dots");
        }

        [Then("No cookie is set for the user")]
        public void ThenNoCookieIsSetForTheUser()
        {
            _cookieContainer.GetAllCookies().Should().BeEmpty();
        }

        [Then("The callback is rejected as invalid")]
        public void ThenTheCallbackIsRejectedAsInvalid()
        {
            _result!.Exception.Should().BeOfType<ApiException<ProblemDetails>>();

            var exception = (ApiException<ProblemDetails>)_result!.Exception!;
            exception.StatusCode.Should().Be(400);
            exception.Result.Title.Should().Be("One or more validation errors occurred.");
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
                .Select(i => i.Arguments[0])
                .OfType<AccountInformation>()
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

        [Then("An access token is not returned")]
        public void ThenAnAccessTokenIsNotReturned()
        {
            _session.OneTimeCode.Should().BeNull("a failed sign-in gives no code");
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