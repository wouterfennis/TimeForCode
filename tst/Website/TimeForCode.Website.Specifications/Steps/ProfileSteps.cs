using FluentAssertions;
using Microsoft.Playwright;
using Reqnroll;

namespace TimeForCode.Website.Specifications.Steps
{
    [Binding]
    internal class ProfileSteps
    {
        private readonly BrowserFixture _browser;

        public ProfileSteps(BrowserFixture browser)
        {
            _browser = browser;
        }

        [When("The visitor navigates to the profile page")]
        public async Task WhenTheVisitorNavigatesToTheProfilePageAsync()
        {
            await _browser.Page.GotoAsync(_browser.BaseUrl + "/profile");
        }

        [Then("The login handle is visible")]
        public async Task ThenTheLoginHandleIsVisibleAsync()
        {
            var loginHandle = _browser.Page.GetByTestId("login-handle");
            await loginHandle.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
            (await loginHandle.IsVisibleAsync()).Should().BeTrue();
            var text = await loginHandle.InnerTextAsync();
            text.Should().StartWith("@");
        }
    }
}