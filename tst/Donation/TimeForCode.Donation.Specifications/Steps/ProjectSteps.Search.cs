using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Reqnroll;
using TimeForCode.Donation.Api.Client;
using TimeForCode.Donation.Application.Interfaces;
using TimeForCode.Donation.Domain;
using TimeForCode.Donation.Specifications.Mocking;
using TimeForCode.Donation.Specifications.TestBuilder;

namespace TimeForCode.Donation.Specifications.Steps
{
    internal partial class ProjectSteps
    {
        private readonly List<Project> _catalog = [];
        private IReadOnlyList<string>? _firstSearchOrder;
        private IReadOnlyList<string>? _secondSearchOrder;

        [Given("There are active projects with the names {string} and {string}")]
        public void GivenThereAreActiveProjectsWithTheNames(string firstName, string secondName)
        {
            _catalog.Add(ProjectBuilder.BuildActive(1, name: firstName));
            _catalog.Add(ProjectBuilder.BuildActive(2, name: secondName));
        }

        [Given("There are active projects written in {string} and in {string}")]
        public void GivenThereAreActiveProjectsWrittenIn(string firstLanguage, string secondLanguage)
        {
            _catalog.Add(ProjectBuilder.BuildActive(1, language: firstLanguage));
            _catalog.Add(ProjectBuilder.BuildActive(2, language: secondLanguage));
        }

        [Given("There are active projects tagged {string} and {string}")]
        public void GivenThereAreActiveProjectsTagged(string firstTag, string secondTag)
        {
            _catalog.Add(ProjectBuilder.BuildActive(1, topics: [firstTag]));
            _catalog.Add(ProjectBuilder.BuildActive(2, topics: [secondTag]));
        }

        [Given("There are active projects that match only some of the text, language and tag criteria")]
        public void GivenThereAreActiveProjectsThatMatchOnlySomeOfTheCriteria()
        {
            _catalog.Add(ProjectBuilder.BuildActive(1, name: "Game Planner", language: "C#", topics: ["gaming"]));
            _catalog.Add(ProjectBuilder.BuildActive(2, name: "Game Planner", language: "Rust", topics: ["gaming"]));
            _catalog.Add(ProjectBuilder.BuildActive(3, name: "Game Planner", language: "C#", topics: ["accessibility"]));
            _catalog.Add(ProjectBuilder.BuildActive(4, name: "Garden Tracker", language: "C#", topics: ["gaming"]));
        }

        [Given("There are {int} active projects on the time for code platform")]
        public void GivenThereAreActiveProjects(int count)
        {
            for (var number = 1; number <= count; number++)
            {
                _catalog.Add(ProjectBuilder.BuildActive(number));
            }
        }

        [Given("There are active projects created at different moments")]
        public void GivenThereAreActiveProjectsCreatedAtDifferentMoments()
        {
            _catalog.Add(ProjectBuilder.BuildActive(1, publishedAt: new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero)));
            _catalog.Add(ProjectBuilder.BuildActive(2, publishedAt: new DateTimeOffset(2024, 3, 1, 0, 0, 0, TimeSpan.Zero)));
            _catalog.Add(ProjectBuilder.BuildActive(3, publishedAt: new DateTimeOffset(2024, 2, 1, 0, 0, 0, TimeSpan.Zero)));
        }

        [Given("There are active projects created at the same moment")]
        public void GivenThereAreActiveProjectsCreatedAtTheSameMoment()
        {
            var moment = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
            for (var number = 1; number <= 4; number++)
            {
                _catalog.Add(ProjectBuilder.BuildActive(number, publishedAt: moment));
            }
        }

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

        [Then("The user is informed the search criteria are invalid")]
        public void ThenTheUserIsInformedTheSearchCriteriaAreInvalid()
        {
            _exception.Should().NotBeNull();
            _exception!.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        }

        [Then("The total number of matching projects is included")]
        public void ThenTheTotalNumberOfMatchingProjectsIsIncluded()
        {
            _getProjectsResult.Should().NotBeNull();
            _getProjectsResult!.TotalCount.Should().BeGreaterThan(0);
        }

        [Then("The total number of matching projects is zero")]
        public void ThenTheTotalNumberOfMatchingProjectsIsZero()
        {
            ThenTheTotalNumberOfMatchingProjectsIs(0);
        }

        [Then("The total number of matching projects is {int}")]
        public void ThenTheTotalNumberOfMatchingProjectsIs(int expected)
        {
            _getProjectsResult.Should().NotBeNull();
            _getProjectsResult!.TotalCount.Should().Be(expected);
        }

        [Then("^The project that is (in draft|pending approval|archived) is not included in the results$")]
        public void ThenTheProjectThatIsNotActiveIsNotIncludedInTheResults(string state)
        {
            _exception.Should().BeNull();
            _getProjectsResult.Should().NotBeNull();
            _getProjectsResult!.Projects.Select(p => p.Id).Should().NotContain(_lifecycleProject!.Id.ToString());
        }

        [Then("Only the project {string} is included in the results")]
        public void ThenOnlyTheProjectIsIncludedInTheResults(string name)
        {
            _exception.Should().BeNull();
            _getProjectsResult.Should().NotBeNull();
            _getProjectsResult!.Projects.Select(p => p.Name).Should().Equal(name);
        }

        [Then("Only projects written in {string} are included in the results")]
        public void ThenOnlyProjectsWrittenInAreIncludedInTheResults(string language)
        {
            _exception.Should().BeNull();
            _getProjectsResult.Should().NotBeNull();
            _getProjectsResult!.Projects.Should().NotBeEmpty();
            _getProjectsResult.Projects.Should().OnlyContain(p => p.Language == language);
        }

        [Then("Only projects tagged {string} are included in the results")]
        public void ThenOnlyProjectsTaggedAreIncludedInTheResults(string tag)
        {
            _exception.Should().BeNull();
            _getProjectsResult.Should().NotBeNull();
            _getProjectsResult!.Projects.Select(p => p.Id).Should().Equal(
                _catalog.Where(p => p.Snapshot.Topics.Contains(tag)).Select(p => p.Id.ToString()));
        }

        [Then("Only projects matching all criteria are included in the results")]
        public void ThenOnlyProjectsMatchingAllCriteriaAreIncludedInTheResults()
        {
            _exception.Should().BeNull();
            _getProjectsResult.Should().NotBeNull();
            _getProjectsResult!.Projects.Select(p => p.Id).Should().Equal(ProjectBuilder.IdFor(1));
        }

        [Then("An empty list of projects is returned")]
        public void ThenAnEmptyListOfProjectsIsReturned()
        {
            _exception.Should().BeNull();
            _getProjectsResult.Should().NotBeNull();
            _getProjectsResult!.Projects.Should().BeEmpty();
        }

        [Then("{int} projects are returned")]
        public void ThenProjectsAreReturned(int count)
        {
            _exception.Should().BeNull();
            _getProjectsResult.Should().NotBeNull();
            _getProjectsResult!.Projects.Should().HaveCount(count);
        }

        [Then("The projects are ordered from newest to oldest")]
        public void ThenTheProjectsAreOrderedFromNewestToOldest()
        {
            _exception.Should().BeNull();
            _getProjectsResult.Should().NotBeNull();
            _getProjectsResult!.Projects.Select(p => p.Id).Should().Equal(
                ProjectBuilder.IdFor(2), ProjectBuilder.IdFor(3), ProjectBuilder.IdFor(1));
        }

        [Then("Both searches return the projects in the same order")]
        public void ThenBothSearchesReturnTheProjectsInTheSameOrder()
        {
            _firstSearchOrder.Should().NotBeNullOrEmpty();
            _secondSearchOrder.Should().Equal(_firstSearchOrder);
        }

        [Then("The user is informed the paging request is invalid")]
        public void ThenTheUserIsInformedThePagingRequestIsInvalid()
        {
            _exception.Should().NotBeNull();
            _exception!.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        }

        private List<string> ResultIds()
        {
            return _getProjectsResult!.Projects.Select(p => p.Id).ToList();
        }

        private async Task SearchAsync(string? text, string? language, string? tag, int? pageNumber, int? pageSize)
        {
            var catalog = _lifecycleProject == null ? _catalog : [.. _catalog, _lifecycleProject];
            var mockProjectRepository = _provider.GetRequiredService<Mock<IProjectRepository>>();
            mockProjectRepository.Setup(x => x.SearchActiveAsync(It.IsAny<ProjectSearchCriteria>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((ProjectSearchCriteria criteria, CancellationToken _) => InMemoryProjectSearch.Execute(catalog, criteria));

            try
            {
                _getProjectsResult = await _donationClient.GetProjectsAsync(pageNumber, pageSize, text, language, tag);
            }
            catch (ApiException<ProblemDetails> exception)
            {
                _exception = exception;
            }
            catch (ApiException exception)
            {
                _exception = exception;
            }
        }
    }
}