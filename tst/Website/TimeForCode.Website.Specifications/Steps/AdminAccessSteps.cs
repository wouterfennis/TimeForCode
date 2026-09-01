using FluentAssertions;
using Microsoft.Playwright;
using Reqnroll;

namespace TimeForCode.Website.Specifications.Steps
{
    /// <summary>
    /// See <see cref="AdminLoginSteps"/> for the virtual-authenticator rationale — the same approach is
    /// used here to get into an authenticated-as-admin state before exercising /admin access rules.
    /// </summary>
    [Binding]
    internal class AdminAccessSteps
    {
        private const string BootstrapSecret = "test-bootstrap-secret";

        private readonly BrowserFixture _browser;

        public AdminAccessSteps(BrowserFixture browser)
        {
            _browser = browser;
        }

        [Given("The user is logged in as admin")]
        public async Task GivenTheUserIsLoggedInAsAdminAsync()
        {
            var cdpSession = await _browser.Context.NewCDPSessionAsync(_browser.Page);
            await cdpSession.SendAsync("WebAuthn.enable", new Dictionary<string, object> { ["enableUI"] = false });
            await cdpSession.SendAsync("WebAuthn.addVirtualAuthenticator", new Dictionary<string, object>
            {
                ["options"] = new Dictionary<string, object>
                {
                    ["protocol"] = "ctap2",
                    ["transport"] = "internal",
                    ["hasResidentKey"] = true,
                    ["hasUserVerification"] = true,
                    ["isUserVerified"] = true,
                    ["automaticPresenceSimulation"] = true
                }
            });

            await _browser.Page.GotoAsync(_browser.BaseUrl + "/");
            _browser.Page.Dialog += async (_, dialog) => await dialog.AcceptAsync(BootstrapSecret);

            var adminLoginLink = _browser.Page.GetByTestId("admin-login-link");
            await adminLoginLink.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
            await adminLoginLink.ClickAsync();

            await _browser.Page.WaitForURLAsync(url => url.TrimEnd('/').EndsWith("/admin", StringComparison.OrdinalIgnoreCase), new PageWaitForURLOptions { Timeout = 15_000 });
        }

        [When("The visitor navigates to the admin landing page")]
        public async Task WhenTheVisitorNavigatesToTheAdminLandingPageAsync()
        {
            await _browser.Page.GotoAsync(_browser.BaseUrl + "/admin");
        }

        [Then("The admin landing page is visible")]
        public async Task ThenTheAdminLandingPageIsVisibleAsync()
        {
            var adminLandingPage = _browser.Page.GetByTestId("admin-landing-page");
            await adminLandingPage.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
            (await adminLandingPage.IsVisibleAsync()).Should().BeTrue();
        }

        [Then("The visitor sees an unauthorized message")]
        public async Task ThenTheVisitorSeesAnUnauthorizedMessageAsync()
        {
            var unauthorizedMessage = _browser.Page.GetByTestId("unauthorized-message");
            await unauthorizedMessage.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
            (await unauthorizedMessage.IsVisibleAsync()).Should().BeTrue();
        }
    }
}
