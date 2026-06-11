using FluentAssertions;
using Microsoft.Playwright;
using Reqnroll;

namespace TimeForCode.Website.Specifications.Steps
{
    [Binding]
    internal class HomeSteps
    {
        private readonly BrowserFixture _browser;

        public HomeSteps(BrowserFixture browser)
        {
            _browser = browser;
        }

        [When("The visitor navigates to the home page")]
        public async Task WhenTheVisitorNavigatesToTheHomePageAsync()
        {
            await _browser.Page.GotoAsync(_browser.BaseUrl + "/");
        }

        [Then("The login link is visible")]
        public async Task ThenTheLoginLinkIsVisibleAsync()
        {
            var loginLink = _browser.Page.GetByTestId("login-link");
            await loginLink.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
            (await loginLink.IsVisibleAsync()).Should().BeTrue();
        }

        [Then("The published projects section is visible")]
        public async Task ThenThePublishedProjectsSectionIsVisibleAsync()
        {
            // The projects section heading is always rendered; the tile container
            // appears only when there are published projects.
            var heading = _browser.Page.Locator("h2", new PageLocatorOptions { HasTextString = "Published Projects" });
            await heading.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
            (await heading.IsVisibleAsync()).Should().BeTrue();
        }
    }
}