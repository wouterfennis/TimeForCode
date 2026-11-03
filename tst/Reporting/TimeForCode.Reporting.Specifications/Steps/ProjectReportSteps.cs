using AwesomeAssertions;
using Reqnroll;
using System.Net;
using System.Text.Json;

namespace TimeForCode.Reporting.Specifications.Steps
{
    /// <summary>Requesting the detailed donation report and the public summary of the project, and what they contain.</summary>
    [Binding]
    internal class ProjectReportSteps
    {
        private readonly HttpClient _httpClient;
        private readonly ReportingContext _context;

        public ProjectReportSteps(HttpClient httpClient, ReportingContext context)
        {
            _httpClient = httpClient;
            _context = context;
        }

        [When("The user requests the donation report of the project")]
        public Task WhenTheUserRequestsTheDonationReportOfTheProjectAsync()
        {
            return _context.GetAsync(_httpClient, $"api/v1/reporting/project/{_context.ProjectId}/donation-report");
        }

        [When("The user requests the public summary of the project")]
        public Task WhenTheUserRequestsThePublicSummaryOfTheProjectAsync()
        {
            return _context.GetAsync(_httpClient, $"api/v1/reporting/project/{_context.ProjectId}/public-summary");
        }

        [Then("The report shows {int} hours received and {int} hours logged")]
        public void ThenTheReportShowsHoursReceivedAndHoursLogged(int received, int logged)
        {
            JsonElement report = Report();

            report.GetProperty("hoursReceived").GetInt32().Should().Be(received);
            report.GetProperty("hoursLogged").GetInt32().Should().Be(logged);
        }

        [Then("The donation report shows a progress of {int} percent")]
        public void ThenTheDonationReportShowsAProgressOfPercent(int percent)
        {
            Report().GetProperty("progressPercentage").GetInt32().Should().Be(percent);
        }

        [Then("The donation report lists contributor {string} with {int} hours")]
        public void ThenTheDonationReportListsContributorWithHours(string login, int hours)
        {
            JsonElement[] contributors = Report().GetProperty("contributors").EnumerateArray().ToArray();

            contributors
                .Where(c => c.GetProperty("contributorLogin").GetString() == login)
                .Select(c => c.GetProperty("hours").GetInt32())
                .Should().Equal(hours);
        }

        [Then("^The donation report shows (\\d+) (active|pending|completed|cancelled) donations?$")]
        public void ThenTheDonationReportShowsDonationsWithStatus(int count, string status)
        {
            Report().GetProperty($"{status}Donations").GetInt32().Should().Be(count);
        }

        [Then("^The public summary shows (\\d+) donors? and (\\d+) contributors?$")]
        public void ThenThePublicSummaryShowsDonorsAndContributors(int donors, int contributors)
        {
            JsonElement summary = Report();

            summary.GetProperty("donorCount").GetInt32().Should().Be(donors);
            summary.GetProperty("contributorCount").GetInt32().Should().Be(contributors);
        }

        [Then("The public summary does not mention contributor {string}")]
        public void ThenThePublicSummaryDoesNotMentionContributor(string login)
        {
            _context.LastStatusCode.Should().Be(HttpStatusCode.OK);
            _context.LastBody.Should().NotContainEquivalentOf(login);
        }

        private JsonElement Report()
        {
            _context.LastStatusCode.Should().Be(HttpStatusCode.OK);
            return JsonDocument.Parse(_context.LastBody!).RootElement;
        }
    }
}