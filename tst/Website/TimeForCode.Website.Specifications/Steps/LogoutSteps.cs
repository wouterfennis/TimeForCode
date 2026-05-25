using FluentAssertions;
using Microsoft.Playwright;
using Reqnroll;

namespace TimeForCode.Website.Specifications.Steps
{
    [Binding]
    internal class LogoutSteps
    {
        private readonly BrowserFixture _browser;

        public LogoutSteps(BrowserFixture browser)
        {
            _browser = browser;
        }

        [When("The visitor triggers logout")]
        public async Task WhenTheVisitorTriggersLogoutAsync()
        {
            var logoutLink = _browser.Page.GetByTestId("logout-link");
            await logoutLink.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
            await logoutLink.ClickAsync();
        }

        [Then("The logout link is not visible")]
        public async Task ThenTheLogoutLinkIsNotVisibleAsync()
        {
            var logoutLink = _browser.Page.GetByTestId("logout-link");
            await logoutLink.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Hidden });
            (await logoutLink.IsVisibleAsync()).Should().BeFalse();
        }
    }
}