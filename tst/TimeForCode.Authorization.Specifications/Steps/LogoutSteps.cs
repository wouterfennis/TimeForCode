using Reqnroll;
using TimeForCode.Authorization.Api.Client;

namespace TimeForCode.Authorization.Specifications.Steps
{
    [Binding]
    internal class LogoutSteps
    {
        private readonly IAuthClient _authClient;

        public LogoutSteps(IAuthClient authClient)
        {
            _authClient = authClient;
        }

        [Given("The user has not logged in at the time for code platform")]
        public void GivenTheUserHasNotLoggedInAtTheTimeForCodePlatform()
        {
            // do nothing
        }

        [Given("The user has logged in at the time for code platform")]
        public void GivenTheUserHasLoggedInAtTheTimeForCodePlatform()
        {
            throw new PendingStepException();
        }

        [When("The user logs out from the external platform")]
        public async Task WhenTheUserLogsOutFromTheExternalPlatformAsync()
        {
            await _authClient.LogoutAsync();
        }

        [Then("The logout is confirmed")]
        public void ThenTheLogoutIsConfirmed()
        {
            // do nothing, no exceptions should have been thrown
        }

    }
}