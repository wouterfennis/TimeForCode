using FluentAssertions;
using Microsoft.Playwright;
using Reqnroll;

namespace TimeForCode.Website.Specifications.Steps
{
    /// <summary>
    /// Drives the admin passkey ceremony via a Chrome DevTools Protocol virtual authenticator, so the
    /// real <c>IPasskeyHandler&lt;TUser&gt;</c> cryptography on the Authorization API is exercised
    /// end-to-end rather than mocked. Requires the full docker-compose stack to be running (see
    /// TimeForCode.Website.Specifications/README.md) — these scenarios are tagged @E2E and excluded from CI.
    /// </summary>
    [Binding]
    internal class AdminLoginSteps
    {
        private const string BootstrapSecret = "test-bootstrap-secret";

        private readonly BrowserFixture _browser;
        private ICDPSession? _cdpSession;

        public AdminLoginSteps(BrowserFixture browser)
        {
            _browser = browser;
        }

        [Then("The admin login link is visible")]
        public async Task ThenTheAdminLoginLinkIsVisibleAsync()
        {
            var adminLoginLink = _browser.Page.GetByTestId("admin-login-link");
            await adminLoginLink.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
            (await adminLoginLink.IsVisibleAsync()).Should().BeTrue();
        }

        [Given("The time for code platform already has a registered admin credential for this device")]
        public async Task GivenTheTimeForCodePlatformAlreadyHasARegisteredAdminCredentialForThisDeviceAsync()
        {
            await EnableVirtualAuthenticatorAsync();

            // No admin credential exists yet in a fresh environment, so registering once here via the
            // real bootstrap flow is what makes "this device" the registered device for the scenario.
            await _browser.Page.GotoAsync(_browser.BaseUrl + "/");

            _browser.Page.Dialog += async (_, dialog) => await dialog.AcceptAsync(BootstrapSecret);

            var adminLoginLink = _browser.Page.GetByTestId("admin-login-link");
            await adminLoginLink.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
            await adminLoginLink.ClickAsync();

            await _browser.Page.WaitForURLAsync(url => url.TrimEnd('/').EndsWith("/admin", StringComparison.OrdinalIgnoreCase), new PageWaitForURLOptions { Timeout = 15_000 });

            // Return to the home page so the scenario's own "When" steps start from a known location.
            await _browser.Page.GotoAsync(_browser.BaseUrl + "/");
        }

        [When("The user authenticates with the registered admin passkey")]
        public async Task WhenTheUserAuthenticatesWithTheRegisteredAdminPasskeyAsync()
        {
            await EnableVirtualAuthenticatorAsync();

            // The virtual authenticator has automatic presence simulation enabled, so no further
            // interaction is required — navigator.credentials.get() resolves on its own.
            await _browser.Page.WaitForURLAsync(url => url.TrimEnd('/').EndsWith("/admin", StringComparison.OrdinalIgnoreCase), new PageWaitForURLOptions { Timeout = 15_000 });
        }

        [Then("The visitor is redirected to the admin landing page")]
        public async Task ThenTheVisitorIsRedirectedToTheAdminLandingPageAsync()
        {
            var adminLandingPage = _browser.Page.GetByTestId("admin-landing-page");
            await adminLandingPage.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
            (await adminLandingPage.IsVisibleAsync()).Should().BeTrue();
        }

        [Then("The admin navigation is visible")]
        public async Task ThenTheAdminNavigationIsVisibleAsync()
        {
            var adminNavLink = _browser.Page.GetByTestId("admin-nav-link");
            await adminNavLink.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
            (await adminNavLink.IsVisibleAsync()).Should().BeTrue();
        }

        private async Task EnableVirtualAuthenticatorAsync()
        {
            if (_cdpSession != null)
            {
                return;
            }

            _cdpSession = await _browser.Context.NewCDPSessionAsync(_browser.Page);
            await _cdpSession.SendAsync("WebAuthn.enable", new Dictionary<string, object> { ["enableUI"] = false });
            await _cdpSession.SendAsync("WebAuthn.addVirtualAuthenticator", new Dictionary<string, object>
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
        }
    }
}
