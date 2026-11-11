using AwesomeAssertions;
using Reqnroll;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TimeForCode.Reporting.Domain;
using TimeForCode.Reporting.Specifications.Mocking;
using TimeForCode.Reporting.Values;

namespace TimeForCode.Reporting.Specifications.Steps
{
    /// <summary>Badge tiers, the badges of organizations and contributors, and the public statistics.</summary>
    [Binding]
    internal class BadgeSteps
    {
        private const string ProjectId = "507f1f77bcf86cd799439012";

        private readonly HttpClient _httpClient;
        private readonly ReportingContext _context;
        private readonly InMemoryDonationDataSource _donationData;
        private readonly InMemoryBadgeTierRepository _tiers;
        private int _projectNumber;

        public BadgeSteps(HttpClient httpClient, ReportingContext context, InMemoryDonationDataSource donationData, InMemoryBadgeTierRepository tiers)
        {
            _httpClient = httpClient;
            _context = context;
            _donationData = donationData;
            _tiers = tiers;
        }

        private static BadgeRecipientType TypeOf(string recipientType) => Enum.Parse<BadgeRecipientType>(recipientType, ignoreCase: true);

        private static string TierUrl(string recipientType, string name) =>
            $"api/v1/reporting/badge-tier/{TypeOf(recipientType)}/{Uri.EscapeDataString(name)}";

        private string OrganizationBadgesUrl => $"api/v1/reporting/donororganization/{_context.DonorOrganizationId}/badges";

        private static string ContributorBadgesUrl(string login) => $"api/v1/reporting/contributor/{Constants.ContributorIdOf(login)}/badges";

        // Arrange

        [Given("^There is an? (organization|contributor) badge tier \"([^\"]*)\" at (\\d+) hours$")]
        public async Task GivenThereIsABadgeTierAsync(string recipientType, string name, int hours)
        {
            (await _tiers.SaveAsync(new BadgeTier(name, TypeOf(recipientType), hours), CancellationToken.None)).Should().BeTrue();
        }

        [Given("The user has logged {int} more hours that count towards the donation")]
        public void GivenTheUserHasLoggedMoreHoursThatCountTowardsTheDonation(int hours)
        {
            _donationData.AddLoggedHours(Constants.TestDonorOrganizationId, hours);
        }

        [Given("^Contributor \"([^\"]*)\" has logged (\\d+) hours on each of (\\d+) projects$")]
        public void GivenContributorHasLoggedHoursOnEachOfProjects(string login, int hours, int projects)
        {
            for (int i = 1; i <= projects; i++)
            {
                string projectId = (0x500 + i + _projectNumber).ToString("x24");
                _donationData.AddProject(projectId);
                _donationData.AddProjectContribution(projectId, new ProjectContribution(Constants.ContributorIdOf(login), login, hours));
            }

            _projectNumber += projects;
        }

        [Given("There is no contributor with the given identifier")]
        public void GivenThereIsNoContributorWithTheGivenIdentifier()
        {
            // A contributor exists in the Donation context once they logged hours; nothing is arranged for this one.
        }

        [Given("^The platform has (\\d+) donor organizations with (\\d+) logged hours each$")]
        public void GivenThePlatformHasDonorOrganizationsWithLoggedHoursEach(int organizations, int hours)
        {
            for (int i = 1; i <= organizations; i++)
            {
                string organizationId = (0x300 + i).ToString("x24");
                _donationData.AddOrganization(organizationId);
                _donationData.AddDonation(organizationId, new OrganizationDonation(ProjectId, DonationStatus.Active, hours * 2, hours));
            }
        }

        [Given("The platform has no logged hours")]
        public void GivenThePlatformHasNoLoggedHours()
        {
            // A new platform: nothing is arranged.
        }

        // Act

        [When("^The user defines the (organization|contributor) badge tier \"([^\"]*)\" at (\\d+) hours$")]
        public Task WhenTheUserDefinesTheBadgeTierAsync(string recipientType, string name, int hours)
        {
            return _context.SendAsync(() => _httpClient.PutAsJsonAsync(new Uri(TierUrl(recipientType, name), UriKind.Relative), new { thresholdHours = hours }));
        }

        [When("^The user removes the (organization|contributor) badge tier \"([^\"]*)\"$")]
        public Task WhenTheUserRemovesTheBadgeTierAsync(string recipientType, string name)
        {
            return _context.SendAsync(() => _httpClient.DeleteAsync(new Uri(TierUrl(recipientType, name), UriKind.Relative)));
        }

        [Given("The user has requested the badges of the donor organization")]
        [When("The user requests the badges of the donor organization")]
        public Task WhenTheUserRequestsTheBadgesOfTheDonorOrganizationAsync()
        {
            return _context.GetAsync(_httpClient, OrganizationBadgesUrl);
        }

        [Given("The user has requested the badges of Contributor {string}")]
        [When("The user requests the badges of Contributor {string}")]
        public Task WhenTheUserRequestsTheBadgesOfContributorAsync(string login)
        {
            return _context.GetAsync(_httpClient, ContributorBadgesUrl(login));
        }

        [When("The badges of the donor organization are requested by {int} visitors at the same time")]
        public Task WhenTheBadgesOfTheDonorOrganizationAreRequestedAtTheSameTimeAsync(int visitors)
        {
            return RequestAtTheSameTimeAsync(visitors, OrganizationBadgesUrl);
        }

        [When("The badges of Contributor {string} are requested by {int} visitors at the same time")]
        public Task WhenTheBadgesOfContributorAreRequestedAtTheSameTimeAsync(string login, int visitors)
        {
            return RequestAtTheSameTimeAsync(visitors, ContributorBadgesUrl(login));
        }

        [When("The user requests the public statistics")]
        public Task WhenTheUserRequestsThePublicStatisticsAsync()
        {
            return _context.GetAsync(_httpClient, "api/v1/reporting/statistics");
        }

        private async Task RequestAtTheSameTimeAsync(int visitors, string url)
        {
            // Every visitor sees an answer; the last one is kept for the Then-steps. All of them must succeed.
            await Task.WhenAll(Enumerable.Range(0, visitors).Select(_ => _context.GetAsync(_httpClient, url)));
            _context.LastStatusCode.Should().Be(HttpStatusCode.OK);
        }

        // Assert

        [Then("^The (organization|contributor) badge tier \"([^\"]*)\" is listed with a threshold of (\\d+) hours$")]
        public async Task ThenTheBadgeTierIsListedAsync(string recipientType, string name, int hours)
        {
            JsonElement tier = (await ListedTiersAsync(recipientType)).Single(t => t.GetProperty("name").GetString() == name);

            tier.GetProperty("thresholdHours").GetInt32().Should().Be(hours);
        }

        [Then("^The (organization|contributor) badge tier \"([^\"]*)\" is no longer listed$")]
        public async Task ThenTheBadgeTierIsNoLongerListedAsync(string recipientType, string name)
        {
            (await ListedTiersAsync(recipientType)).Should().NotContain(t => t.GetProperty("name").GetString() == name);
        }

        [Then("The organization has the badge {string}")]
        public void ThenTheOrganizationHasTheBadge(string badge) => BadgeNames().Should().Contain(badge);

        [Then("The organization has the badge {string} exactly once")]
        public void ThenTheOrganizationHasTheBadgeExactlyOnce(string badge) => BadgeNames().Count(n => n == badge).Should().Be(1);

        [Then("The organization has no badges")]
        public void ThenTheOrganizationHasNoBadges() => BadgeNames().Should().BeEmpty();

        [Then("Contributor {string} has the badge {string}")]
        public void ThenContributorHasTheBadge(string login, string badge) => BadgeNames().Should().Contain(badge);

        [Then("Contributor {string} has the badge {string} exactly once")]
        public void ThenContributorHasTheBadgeExactlyOnce(string login, string badge) => BadgeNames().Count(n => n == badge).Should().Be(1);

        [Then("Contributor {string} has no badges")]
        public void ThenContributorHasNoBadges(string login) => BadgeNames().Should().BeEmpty();

        [Then("The response contains only badge names and award dates")]
        public void ThenTheResponseContainsOnlyBadgeNamesAndAwardDates()
        {
            JsonElement root = Body();

            root.EnumerateObject().Select(p => p.Name).Should().Equal("badges");
            foreach (JsonElement badge in root.GetProperty("badges").EnumerateArray())
            {
                badge.EnumerateObject().Select(p => p.Name).Should().BeEquivalentTo(["name", "awardedAt"]);
            }
        }

        [Then("The public statistics show {int} hours and {int} organizations supported")]
        public void ThenThePublicStatisticsShow(int hours, int organizations)
        {
            JsonElement statistics = Body();

            statistics.GetProperty("totalHoursLogged").GetInt64().Should().Be(hours);
            statistics.GetProperty("organizationsSupported").GetInt32().Should().Be(organizations);
        }

        [Then("The public statistics contain only totals")]
        public void ThenThePublicStatisticsContainOnlyTotals()
        {
            Body().EnumerateObject().Select(p => p.Name).Should().BeEquivalentTo(["totalHoursLogged", "organizationsSupported"]);
        }

        private JsonElement Body()
        {
            _context.LastStatusCode.Should().Be(HttpStatusCode.OK);
            return JsonDocument.Parse(_context.LastBody!).RootElement;
        }

        private List<string> BadgeNames() =>
            Body().GetProperty("badges").EnumerateArray().Select(b => b.GetProperty("name").GetString()!).ToList();

        // The tier list is public, so reading it back does not need (and must not disturb) the identity of the scenario.
        private async Task<List<JsonElement>> ListedTiersAsync(string recipientType)
        {
            using HttpResponseMessage response = await _httpClient.GetAsync(new Uri($"api/v1/reporting/badge-tier?recipientType={TypeOf(recipientType)}", UriKind.Relative));
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            JsonElement body = await response.Content.ReadFromJsonAsync<JsonElement>();
            return body.GetProperty("tiers").EnumerateArray().Select(t => t.Clone()).ToList();
        }
    }
}