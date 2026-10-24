using FluentAssertions;
using Moq;
using Reqnroll;
using TimeForCode.Donation.Api.Client;
using TimeForCode.Donation.Application.Interfaces;
using TimeForCode.Donation.Domain;
using TimeForCode.Donation.Specifications.Mocking;
using TimeForCode.Donation.Specifications.TestBuilder;

namespace TimeForCode.Donation.Specifications.Steps
{
    /// <summary>Listing and searching the published projects.</summary>
    [Binding]
    internal class ProjectSearchSteps
    {
        private readonly IDonationClient _donationClient;
        private readonly DonationContext _context;
        private readonly DonationFixtures _fixtures;

        private IReadOnlyList<string>? _firstSearchOrder;
        private IReadOnlyList<string>? _secondSearchOrder;

        public ProjectSearchSteps(IDonationClient donationClient, DonationContext context, DonationFixtures fixtures)
        {
            _donationClient = donationClient;
            _context = context;
            _fixtures = fixtures;
        }

        [Given("There are published projects on the time for code platform")]
        public void GivenThereArePublishedProjectsOnTheTimeForCodePlatform()
        {
            _context.Catalog.Add(ProjectBuilder.BuildActive(ProjectBuilder.IdFor(100)));
        }

        [Given("One of the projects has been unpublished")]
        public void GivenOneOfTheProjectsHasBeenUnpublished()
        {
            _context.Catalog.ForEach(p => p.Archive());
        }

        [Given("There are active projects with the names {string} and {string}")]
        public void GivenThereAreActiveProjectsWithTheNames(string firstName, string secondName)
        {
            _context.Catalog.Add(ProjectBuilder.BuildActive(1, name: firstName));
            _context.Catalog.Add(ProjectBuilder.BuildActive(2, name: secondName));
        }

        [Given("There are active projects written in {string} and in {string}")]
        public void GivenThereAreActiveProjectsWrittenIn(string firstLanguage, string secondLanguage)
        {
            _context.Catalog.Add(ProjectBuilder.BuildActive(1, language: firstLanguage));
            _context.Catalog.Add(ProjectBuilder.BuildActive(2, language: secondLanguage));
        }

        [Given("There are active projects tagged {string} and {string}")]
        public void GivenThereAreActiveProjectsTagged(string firstTag, string secondTag)
        {
            _context.Catalog.Add(ProjectBuilder.BuildActive(1, topics: [firstTag]));
            _context.Catalog.Add(ProjectBuilder.BuildActive(2, topics: [secondTag]));
        }

        [Given("There are active projects that match only some of the text, language and tag criteria")]
        public void GivenThereAreActiveProjectsThatMatchOnlySomeOfTheCriteria()
        {
            _context.Catalog.Add(ProjectBuilder.BuildActive(1, name: "Game Planner", language: "C#", topics: ["gaming"]));
            _context.Catalog.Add(ProjectBuilder.BuildActive(2, name: "Game Planner", language: "Rust", topics: ["gaming"]));
            _context.Catalog.Add(ProjectBuilder.BuildActive(3, name: "Game Planner", language: "C#", topics: ["accessibility"]));
            _context.Catalog.Add(ProjectBuilder.BuildActive(4, name: "Garden Tracker", language: "C#", topics: ["gaming"]));
        }

        [Given("There are {int} active projects on the time for code platform")]
        public void GivenThereAreActiveProjects(int count)
        {
            for (var number = 1; number <= count; number++)
            {
                _context.Catalog.Add(ProjectBuilder.BuildActive(number));
            }
        }

        [Given("There are active projects created at different moments")]
        public void GivenThereAreActiveProjectsCreatedAtDifferentMoments()
        {
            _context.Catalog.Add(ProjectBuilder.BuildActive(1, publishedAt: new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero)));
            _context.Catalog.Add(ProjectBuilder.BuildActive(2, publishedAt: new DateTimeOffset(2024, 3, 1, 0, 0, 0, TimeSpan.Zero)));
            _context.Catalog.Add(ProjectBuilder.BuildActive(3, publishedAt: new DateTimeOffset(2024, 2, 1, 0, 0, 0, TimeSpan.Zero)));
        }

        [Given("There are active projects created at the same moment")]
        public void GivenThereAreActiveProjectsCreatedAtTheSameMoment()
        {
            var moment = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
            for (var number = 1; number <= 4; number++)
            {
                _context.Catalog.Add(ProjectBuilder.BuildActive(number, publishedAt: moment));
            }
        }

        [When("The user requests the list of published projects")]
        [When("The user requests the list of published projects without an account")]
        [When("The user searches the projects without criteria")]
        [When("The user searches the projects without an account")]
        public Task WhenTheUserSearchesTheProjectsWithoutCriteriaAsync()
        {
            return SearchAsync(null, null, null, null, null);
        }

        [When("The user searches the projects for the text {string}")]
        public Task WhenTheUserSearchesTheProjectsForTheTextAsync(string text)
        {
            return SearchAsync(text, null, null, null, null);
        }

        [When("The user filters the projects by the language {string}")]
        public Task WhenTheUserFiltersTheProjectsByTheLanguageAsync(string language)
        {
            return SearchAsync(null, language, null, null, null);
        }

        [When("The user filters the projects by the tag {string}")]
        public Task WhenTheUserFiltersTheProjectsByTheTagAsync(string tag)
        {
            return SearchAsync(null, null, tag, null, null);
        }

        [When("The user searches the projects for the text {string} with the language {string} and the tag {string}")]
        public Task WhenTheUserSearchesTheProjectsForTheTextWithTheLanguageAndTheTagAsync(string text, string language, string tag)
        {
            return SearchAsync(text, language, tag, null, null);
        }

        [When("The user requests page {int} of the projects with {int} projects per page")]
        public Task WhenTheUserRequestsPageOfTheProjectsAsync(int pageNumber, int pageSize)
        {
            return SearchAsync(null, null, null, pageNumber, pageSize);
        }

        [When("The user searches the projects twice without criteria")]
        public async Task WhenTheUserSearchesTheProjectsTwiceWithoutCriteriaAsync()
        {
            await SearchAsync(null, null, null, null, null);
            _firstSearchOrder = ResultIds();

            await SearchAsync(null, null, null, null, null);
            _secondSearchOrder = ResultIds();
        }

        [When("^The user searches the projects with a (text|language|tag) of 101 characters$")]
        public Task WhenTheUserSearchesTheProjectsWithACriterionThatIsTooLongAsync(string criterion)
        {
            var tooLong = new string('a', 101);
            return SearchAsync(
                criterion == "text" ? tooLong : null,
                criterion == "language" ? tooLong : null,
                criterion == "tag" ? tooLong : null,
                null,
                null);
        }

        [Then("A paginated list of projects is returned")]
        public void ThenAPaginatedListOfProjectsIsReturned()
        {
            _context.LastException.Should().BeNull();
            _context.ProjectsResult.Should().NotBeNull();
            _context.ProjectsResult!.Projects.Should().NotBeNull();
        }

        [Then("Each project includes a link to the original GitHub repository")]
        public void ThenEachProjectIncludesALinkToTheOriginalGitHubRepository()
        {
            _context.ProjectsResult.Should().NotBeNull();
            _context.ProjectsResult!.Projects.Should().NotBeEmpty();
            _context.ProjectsResult!.Projects.Should().AllSatisfy(p => p.GithubUrl.Should().NotBeNullOrEmpty());
        }

        [Then("Each project summary includes stars, forks, and open issues counts")]
        public void ThenEachProjectSummaryIncludesStarsForksAndOpenIssuesCounts()
        {
            var expectedSnapshot = ProjectBuilder.BuildSnapshot();

            _context.ProjectsResult.Should().NotBeNull();
            _context.ProjectsResult!.Projects.Should().NotBeEmpty();
            _context.ProjectsResult!.Projects.Should().AllSatisfy(p =>
            {
                p.StargazersCount.Should().Be(expectedSnapshot.StargazersCount);
                p.ForksCount.Should().Be(expectedSnapshot.ForksCount);
                p.OpenIssuesCount.Should().Be(expectedSnapshot.OpenIssuesCount);
            });
        }

        [Then("The archived project is not included in the results")]
        public void ThenTheArchivedProjectIsNotIncludedInTheResults()
        {
            _context.LastException.Should().BeNull();
            _context.ProjectsResult.Should().NotBeNull();
            _context.ProjectsResult!.Projects.Should().BeEmpty();
        }

        [Then("The total number of matching projects is included")]
        public void ThenTheTotalNumberOfMatchingProjectsIsIncluded()
        {
            _context.ProjectsResult.Should().NotBeNull();
            _context.ProjectsResult!.TotalCount.Should().BeGreaterThan(0);
        }

        [Then("The total number of matching projects is zero")]
        public void ThenTheTotalNumberOfMatchingProjectsIsZero()
        {
            ThenTheTotalNumberOfMatchingProjectsIs(0);
        }

        [Then("The total number of matching projects is {int}")]
        public void ThenTheTotalNumberOfMatchingProjectsIs(int expected)
        {
            _context.ProjectsResult.Should().NotBeNull();
            _context.ProjectsResult!.TotalCount.Should().Be(expected);
        }

        [Then("^The project that is (in draft|pending approval|archived) is not included in the results$")]
        public void ThenTheProjectThatIsNotActiveIsNotIncludedInTheResults(string state)
        {
            _context.LastException.Should().BeNull();
            _context.ProjectsResult.Should().NotBeNull();
            _context.ProjectsResult!.Projects.Select(p => p.Id).Should().NotContain(_context.LifecycleProject!.Id.ToString());
        }

        [Then("Only the project {string} is included in the results")]
        public void ThenOnlyTheProjectIsIncludedInTheResults(string name)
        {
            _context.LastException.Should().BeNull();
            _context.ProjectsResult.Should().NotBeNull();
            _context.ProjectsResult!.Projects.Select(p => p.Name).Should().Equal(name);
        }

        [Then("Only projects written in {string} are included in the results")]
        public void ThenOnlyProjectsWrittenInAreIncludedInTheResults(string language)
        {
            _context.LastException.Should().BeNull();
            _context.ProjectsResult.Should().NotBeNull();
            _context.ProjectsResult!.Projects.Should().NotBeEmpty();
            _context.ProjectsResult.Projects.Should().OnlyContain(p => p.Language == language);
        }

        [Then("Only projects tagged {string} are included in the results")]
        public void ThenOnlyProjectsTaggedAreIncludedInTheResults(string tag)
        {
            _context.LastException.Should().BeNull();
            _context.ProjectsResult.Should().NotBeNull();
            _context.ProjectsResult!.Projects.Select(p => p.Id).Should().Equal(
                _context.Catalog.Where(p => p.Snapshot.Topics.Contains(tag)).Select(p => p.Id.ToString()));
        }

        [Then("Only projects matching all criteria are included in the results")]
        public void ThenOnlyProjectsMatchingAllCriteriaAreIncludedInTheResults()
        {
            _context.LastException.Should().BeNull();
            _context.ProjectsResult.Should().NotBeNull();
            _context.ProjectsResult!.Projects.Select(p => p.Id).Should().Equal(ProjectBuilder.IdFor(1));
        }

        [Then("An empty list of projects is returned")]
        public void ThenAnEmptyListOfProjectsIsReturned()
        {
            _context.LastException.Should().BeNull();
            if (_context.MyProjects != null)
            {
                _context.MyProjects.Projects.Should().BeEmpty();
                return;
            }

            _context.ProjectsResult.Should().NotBeNull();
            _context.ProjectsResult!.Projects.Should().BeEmpty();
        }

        [Then("{int} projects are returned")]
        public void ThenProjectsAreReturned(int count)
        {
            _context.LastException.Should().BeNull();
            _context.ProjectsResult.Should().NotBeNull();
            _context.ProjectsResult!.Projects.Should().HaveCount(count);
        }

        [Then("The projects are ordered from newest to oldest")]
        public void ThenTheProjectsAreOrderedFromNewestToOldest()
        {
            _context.LastException.Should().BeNull();
            _context.ProjectsResult.Should().NotBeNull();
            _context.ProjectsResult!.Projects.Select(p => p.Id).Should().Equal(
                ProjectBuilder.IdFor(2), ProjectBuilder.IdFor(3), ProjectBuilder.IdFor(1));
        }

        [Then("Both searches return the projects in the same order")]
        public void ThenBothSearchesReturnTheProjectsInTheSameOrder()
        {
            _firstSearchOrder.Should().NotBeNullOrEmpty();
            _secondSearchOrder.Should().Equal(_firstSearchOrder);
        }

        private List<string> ResultIds()
        {
            return _context.ProjectsResult!.Projects.Select(p => p.Id).ToList();
        }

        private async Task SearchAsync(string? text, string? language, string? tag, int? pageNumber, int? pageSize)
        {
            List<Project> catalog = _context.LifecycleProject == null ? _context.Catalog : [.. _context.Catalog, _context.LifecycleProject];
            _fixtures.ProjectRepository.Setup(x => x.SearchActiveAsync(It.IsAny<ProjectSearchCriteria>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((ProjectSearchCriteria criteria, CancellationToken _) => InMemoryProjectSearch.Execute(catalog, criteria));

            await _context.CaptureAsync(async () => _context.ProjectsResult = await _donationClient.GetProjectsAsync(pageNumber, pageSize, text, language, tag));
        }
    }
}