using AwesomeAssertions;
using Reqnroll;
using System.Text.Json;

namespace TimeForCode.Reporting.Specifications.Steps
{
    /// <summary>Requesting the impact report and the contribution history export of the donor organization, and what they contain.</summary>
    [Binding]
    internal class ReportSteps
    {
        private static readonly string[] ContributionFields = ["id", "projectId", "contributorLogin", "hours", "githubReference", "loggedAt"];

        private readonly HttpClient _httpClient;
        private readonly ReportingContext _context;

        public ReportSteps(HttpClient httpClient, ReportingContext context)
        {
            _httpClient = httpClient;
            _context = context;
        }

        [When("The user requests the impact report of the donor organization")]
        public Task WhenTheUserRequestsTheImpactReportOfTheDonorOrganizationAsync()
        {
            return _context.GetAsync(_httpClient, $"api/v1/reporting/donororganization/{_context.DonorOrganizationId}/impact-report");
        }

        [When("^The user exports the contribution history of the donor organization as (CSV|JSON)$")]
        public Task WhenTheUserExportsTheContributionHistoryOfTheDonorOrganizationAsync(string format)
        {
            return _context.GetAsync(_httpClient, $"api/v1/reporting/donororganization/{_context.DonorOrganizationId}/contribution-history?format={format.ToLowerInvariant()}");
        }

        [Then("The impact report shows {int} hours pledged and {int} hours logged")]
        public void ThenTheImpactReportShowsHoursPledgedAndHoursLogged(int pledged, int logged)
        {
            JsonElement report = ImpactReport();

            report.GetProperty("hoursPledged").GetInt32().Should().Be(pledged);
            report.GetProperty("hoursLogged").GetInt32().Should().Be(logged);
        }

        [Then("^The impact report shows (\\d+) projects? supported$")]
        public void ThenTheImpactReportShowsProjectsSupported(int projects)
        {
            ImpactReport().GetProperty("projectsSupported").GetInt32().Should().Be(projects);
        }

        [Then("Each project in the impact report shows its hours pledged and hours logged")]
        public void ThenEachProjectInTheImpactReportShowsItsHoursPledgedAndHoursLogged()
        {
            JsonElement[] projects = ImpactReport().GetProperty("projects").EnumerateArray().ToArray();

            projects.Should().NotBeEmpty();
            projects.Select(p => string.Join(",", p.EnumerateObject().Select(property => property.Name)))
                .Should().AllBe("projectId,hoursPledged,hoursLogged");
        }

        [Then("The export contains the documented columns")]
        public void ThenTheExportContainsTheDocumentedColumns()
        {
            _context.LastStatusCode.Should().Be(System.Net.HttpStatusCode.OK);
            _context.LastContentType.Should().Be("text/csv");
            CsvLines().First().Should().Be(Constants.ContributionHistoryCsvHeader);
        }

        [Then("The export contains the documented fields")]
        public void ThenTheExportContainsTheDocumentedFields()
        {
            _context.LastStatusCode.Should().Be(System.Net.HttpStatusCode.OK);
            _context.LastContentType.Should().Be("application/json");

            JsonElement first = ExportedContributions().First();
            first.EnumerateObject().Select(p => p.Name).Should().BeEquivalentTo(ContributionFields);
        }

        [Then("The export lists a contribution of {int} hours")]
        public void ThenTheExportListsAContributionOfHours(int hours)
        {
            _context.LastStatusCode.Should().Be(System.Net.HttpStatusCode.OK);

            IEnumerable<int> exported = _context.LastContentType == "text/csv"
                ? CsvLines().Skip(1).Select(line => int.Parse(line.Split(',')[3], System.Globalization.CultureInfo.InvariantCulture))
                : ExportedContributions().Select(c => c.GetProperty("hours").GetInt32());

            exported.Should().Contain(hours);
        }

        [Then("The export lists no contributions")]
        public void ThenTheExportListsNoContributions()
        {
            _context.LastStatusCode.Should().Be(System.Net.HttpStatusCode.OK);

            int count = _context.LastContentType == "text/csv"
                ? CsvLines().Skip(1).Count()
                : ExportedContributions().Count();

            count.Should().Be(0);
        }

        private JsonElement ImpactReport()
        {
            _context.LastStatusCode.Should().Be(System.Net.HttpStatusCode.OK);
            return JsonDocument.Parse(_context.LastBody!).RootElement;
        }

        private IEnumerable<JsonElement> ExportedContributions()
        {
            return JsonDocument.Parse(_context.LastBody!).RootElement.GetProperty("contributions").EnumerateArray().ToArray();
        }

        private IEnumerable<string> CsvLines()
        {
            return _context.LastBody!.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }
    }
}