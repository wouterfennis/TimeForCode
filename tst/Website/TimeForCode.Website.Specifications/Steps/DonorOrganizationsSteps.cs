using FluentAssertions;
using Microsoft.Playwright;
using Reqnroll;

namespace TimeForCode.Website.Specifications.Steps
{
    [Binding]
    internal class DonorOrganizationsSteps
    {
        private readonly BrowserFixture _browser;

        public DonorOrganizationsSteps(BrowserFixture browser)
        {
            _browser = browser;
        }

        [When("The visitor navigates to the donor organizations page")]
        public async Task WhenTheVisitorNavigatesToTheDonorOrganizationsPageAsync()
        {
            await _browser.Page.GotoAsync(_browser.BaseUrl + "/admin/donor-organizations");
        }

        [Then("The donor organizations heading is visible")]
        public async Task ThenTheDonorOrganizationsHeadingIsVisibleAsync()
        {
            var heading = _browser.Page.GetByTestId("donor-organizations-heading");
            await heading.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
            (await heading.IsVisibleAsync()).Should().BeTrue();
        }

        [Then("The add donor organization button is visible")]
        public async Task ThenTheAddDonorOrganizationButtonIsVisibleAsync()
        {
            var button = _browser.Page.GetByTestId("add-donor-org-button");
            await button.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
            (await button.IsVisibleAsync()).Should().BeTrue();
        }

        [When("The visitor clicks the add donor organization button")]
        public async Task WhenTheVisitorClicksTheAddDonorOrganizationButtonAsync()
        {
            var button = _browser.Page.GetByTestId("add-donor-org-button");
            await button.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
            await button.ClickAsync();
        }

        [Then("The donor organization form is visible")]
        public async Task ThenTheDonorOrganizationFormIsVisibleAsync()
        {
            var form = _browser.Page.GetByTestId("donor-org-form");
            await form.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
            (await form.IsVisibleAsync()).Should().BeTrue();
        }

        [When("The visitor cancels the donor organization form")]
        public async Task WhenTheVisitorCancelsTheDonorOrganizationFormAsync()
        {
            var cancelButton = _browser.Page.GetByTestId("donor-org-cancel-button");
            await cancelButton.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
            await cancelButton.ClickAsync();
        }

        [Then("The donor organization form is not visible")]
        public async Task ThenTheDonorOrganizationFormIsNotVisibleAsync()
        {
            var form = _browser.Page.GetByTestId("donor-org-form");
            await form.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Hidden });
            (await form.IsVisibleAsync()).Should().BeFalse();
        }
    }
}