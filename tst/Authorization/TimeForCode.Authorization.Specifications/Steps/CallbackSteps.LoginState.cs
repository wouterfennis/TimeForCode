using Microsoft.AspNetCore.Mvc.Testing.Handlers;
using Reqnroll;
using System.Net;
using TimeForCode.Authorization.Api.Client;
using TimeForCode.Authorization.Api.Client.Extensions;
using TimeForCode.Authorization.Specifications.Mocking;

namespace TimeForCode.Authorization.Specifications.Steps
{
    internal partial class CallbackSteps
    {
        [When("The external platform calls another instance of the time for code platform to complete the authorization")]
        public async Task WhenTheExternalPlatformCallsAnotherInstanceToCompleteTheAuthorizationAsync()
        {
            _infrastructure.CallbackClient = StartOtherInstanceClient(_infrastructure, _cookieContainer);

            await WhenTheExternalPlatformCallsTheTimeForCodePlatformToCompleteTheAuthorizationAsync();
        }

        [Given("The time for code platform has restarted")]
        public void GivenTheTimeForCodePlatformHasRestarted()
        {
            // A restart loses everything held in memory; the new instance only shares what is stored in the database.
            _infrastructure.CallbackClient = StartOtherInstanceClient(_infrastructure, _cookieContainer);
        }

        [Given("The external platform has already completed the authorization")]
        public async Task GivenTheExternalPlatformHasAlreadyCompletedTheAuthorizationAsync()
        {
            await WhenTheExternalPlatformCallsTheTimeForCodePlatformToCompleteTheAuthorizationAsync();
        }

        [Given("More than 10 minutes have passed since the user logged in at the time for code platform")]
        public void GivenMoreThan10MinutesHavePassedSinceTheUserLoggedIn()
        {
            _infrastructure.Time.Advance(TimeSpan.FromMinutes(10) + TimeSpan.FromSeconds(1));
        }

        private static AuthClient StartOtherInstanceClient(ScenarioInfrastructure infrastructure, CookieContainer cookieContainer)
        {
            var httpClient = infrastructure.StartOtherInstance()
                .CreateDefaultClient(new BearerTokenFromCookieHandler(cookieContainer), new CookieContainerHandler(cookieContainer));

            return new AuthClient(httpClient);
        }
    }
}