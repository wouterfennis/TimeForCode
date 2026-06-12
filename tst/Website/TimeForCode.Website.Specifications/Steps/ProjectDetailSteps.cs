using FluentAssertions;
using Microsoft.Playwright;
using Reqnroll;

namespace TimeForCode.Website.Specifications.Steps
{
    [Binding]
    internal class ProjectDetailSteps
    {
        private readonly BrowserFixture _browser;
        private string? _projectDetailUrl;

        public ProjectDetailSteps(BrowserFixture browser)
        {
            _browser = browser;
        }

        [Given("A project exists and is published")]
        public async Task GivenAProjectExistsAndIsPublishedAsync()
        {
            // Navigate to the projects list and retrieve the href of the first project link
            await _browser.Page.GotoAsync(_browser.BaseUrl + "/projects");
            var firstLink = _browser.Page.GetByTestId("project-link").First;
            await firstLink.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
            _projectDetailUrl = await firstLink.GetAttributeAsync("href");
            _projectDetailUrl.Should().NotBeNullOrWhiteSpace();
        }

        [When("The visitor navigates to the project detail page")]
        public async Task WhenTheVisitorNavigatesToTheProjectDetailPageAsync()
        {
            await _browser.Page.GotoAsync(_browser.BaseUrl + _projectDetailUrl!);
        }

        [Then("The project heading is visible")]
        public async Task ThenTheProjectHeadingIsVisibleAsync()
        {
            var heading = _browser.Page.GetByTestId("project-heading");
            await heading.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
            (await heading.IsVisibleAsync()).Should().BeTrue();
        }

        [Then("A back link to the projects list is visible")]
        public async Task ThenABackLinkToTheProjectsListIsVisibleAsync()
        {
            var backLink = _browser.Page.GetByTestId("back-link");
            await backLink.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
            (await backLink.IsVisibleAsync()).Should().BeTrue();
            var href = await backLink.GetAttributeAsync("href");
            href.Should().Be("/projects");
        }
    }
}