using FluentAssertions;
using Microsoft.Playwright;
using Reqnroll;

namespace TimeForCode.Website.Specifications.Steps
{
    [Binding]
    internal class LoginSteps
    {
        private readonly BrowserFixture _browser;

        public LoginSteps(BrowserFixture browser)
        {
            _browser = browser;
        }

        [Given("The user is logged in")]
        public async Task GivenTheUserIsLoggedInAsync()
        {
            await _browser.Page.GotoAsync(_browser.BaseUrl + "/");
            await PerformLoginAsync();
        }

        [When("The visitor clicks the login link")]
        public async Task WhenTheVisitorClicksTheLoginLinkAsync()
        {
            var loginLink = _browser.Page.GetByTestId("login-link");
            await loginLink.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
            await loginLink.ClickAsync();
        }

        [When("The OAuth flow completes through the identity provider mock")]
        public async Task WhenTheOAuthFlowCompletesThroughTheIdentityProviderMockAsync()
        {
            var idpBaseUrl = TimeForCode.Website.Specifications.WebsiteTestSettings.IdentityProviderMockBaseUrl.TrimEnd('/');
            await _browser.Page.WaitForURLAsync(url => url.StartsWith(idpBaseUrl, StringComparison.OrdinalIgnoreCase), new PageWaitForURLOptions { Timeout = 10_000 });

            // The identity provider mock shows an "Authorize" button; clicking it
            // posts the confirmation form and redirects back to the website.
            var authorizeButton = _browser.Page.Locator("role=button[name=\"Authorize\"]");
            await authorizeButton.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
            await authorizeButton.ClickAsync();
        }

        [Then("The visitor is returned to the home page")]
        public async Task ThenTheVisitorIsReturnedToTheHomePageAsync()
        {
            await _browser.Page.WaitForURLAsync(url => url.TrimEnd('/') == _browser.BaseUrl || url.StartsWith(_browser.BaseUrl + "/?"), new PageWaitForURLOptions { Timeout = 10_000 });
        }

        [Then("The user name is visible")]
        public async Task ThenTheUserNameIsVisibleAsync()
        {
            var userName = _browser.Page.GetByTestId("user-name");
            await userName.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
            (await userName.IsVisibleAsync()).Should().BeTrue();
        }

        [Then("The logout link is visible")]
        public async Task ThenTheLogoutLinkIsVisibleAsync()
        {
            var logoutLink = _browser.Page.GetByTestId("logout-link");
            await logoutLink.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
            (await logoutLink.IsVisibleAsync()).Should().BeTrue();
        }

        internal async Task PerformLoginAsync()
        {
            var loginLink = _browser.Page.GetByTestId("login-link");
            await loginLink.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
            await loginLink.ClickAsync();

            var idpBaseUrl = TimeForCode.Website.Specifications.WebsiteTestSettings.IdentityProviderMockBaseUrl.TrimEnd('/');
            await _browser.Page.WaitForURLAsync(url => url.StartsWith(idpBaseUrl, StringComparison.OrdinalIgnoreCase), new PageWaitForURLOptions { Timeout = 10_000 });

            var authorizeButton = _browser.Page.Locator("role=button[name=\"Authorize\"]");
            await authorizeButton.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
            await authorizeButton.ClickAsync();

            await _browser.Page.WaitForURLAsync(url => url.TrimEnd('/') == _browser.BaseUrl || url.StartsWith(_browser.BaseUrl + "/?"), new PageWaitForURLOptions { Timeout = 10_000 });
        }
    }
}