using FluentAssertions;
using Microsoft.Playwright;
using Reqnroll;

namespace TimeForCode.Website.Specifications.Steps
{
    [Binding]
    internal class ProjectsSteps
    {
        private readonly BrowserFixture _browser;

        public ProjectsSteps(BrowserFixture browser)
        {
            _browser = browser;
        }

        [When("The visitor navigates to the projects page")]
        public async Task WhenTheVisitorNavigatesToTheProjectsPageAsync()
        {
            await _browser.Page.GotoAsync(_browser.BaseUrl + "/projects");
        }

        [Then("At least one project tile is visible")]
        public async Task ThenAtLeastOneProjectTileIsVisibleAsync()
        {
            var tiles = _browser.Page.GetByTestId("project-tile");
            await tiles.First.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
            var count = await tiles.CountAsync();
            count.Should().BeGreaterThan(0);
        }

        [Then("Each project tile links to the project detail page")]
        public async Task ThenEachProjectTileLinksToTheProjectDetailPageAsync()
        {
            var links = _browser.Page.GetByTestId("project-link");
            var count = await links.CountAsync();
            count.Should().BeGreaterThan(0);

            for (int i = 0; i < count; i++)
            {
                var href = await links.Nth(i).GetAttributeAsync("href");
                href.Should().NotBeNullOrWhiteSpace();
                href!.Should().MatchRegex(@"^/projects/\S+");
            }
        }
    }
}