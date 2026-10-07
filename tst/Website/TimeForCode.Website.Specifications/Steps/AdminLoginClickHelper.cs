using Microsoft.Playwright;

namespace TimeForCode.Website.Specifications.Steps
{
    /// <summary>
    /// AdminLoginButton is the only InteractiveServer component on the site, so its @onclick handler only
    /// works once the SignalR circuit has finished connecting — a race a headless browser can lose against
    /// a plain "element is visible" wait. Retries the click until the login/registration ceremony actually
    /// starts (observed as the first fetch to the admin-authentication API), rather than trusting the first
    /// click reached the server.
    /// </summary>
    internal static class AdminLoginClickHelper
    {
        public static async Task ClickUntilCeremonyStartsAsync(IPage page, ILocator adminLoginLink)
        {
            for (var attempt = 0; attempt < 10; attempt++)
            {
                try
                {
                    await adminLoginLink.ClickAsync();
                    await page.WaitForRequestAsync(
                        request => request.Url.Contains("/api/v1/admin-authentication/"),
                        new PageWaitForRequestOptions { Timeout = 1_500 });
                    return;
                }
                catch (TimeoutException)
                {
                }
            }

            throw new TimeoutException("Clicking the admin login link never reached the Blazor circuit.");
        }
    }
}