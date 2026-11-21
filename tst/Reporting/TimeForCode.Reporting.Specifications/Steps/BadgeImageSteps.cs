using AwesomeAssertions;
using Reqnroll;
using System.Net;
using System.Xml.Linq;
using TimeForCode.Reporting.Specifications.Mocking;

namespace TimeForCode.Reporting.Specifications.Steps
{
    /// <summary>The embeddable badge images of projects, donor organizations and contributors.</summary>
    [Binding]
    internal class BadgeImageSteps
    {
        private const string Label = "TimeForCode";
        private const int LimitedCacheSeconds = 300;
        private const int ShortCacheSeconds = 30;

        private readonly HttpClient _httpClient;
        private readonly ReportingContext _context;
        private readonly InMemoryDonationDataSource _donationData;
        private readonly PublicRateLimit _publicRateLimit;

        public BadgeImageSteps(HttpClient httpClient, ReportingContext context, InMemoryDonationDataSource donationData, PublicRateLimit publicRateLimit)
        {
            _httpClient = httpClient;
            _context = context;
            _donationData = donationData;
            _publicRateLimit = publicRateLimit;
        }

        private string ProjectBadgeUrl(string variant) => $"api/v1/reporting/project/{_context.ProjectId}/badge/{variant.Replace(' ', '-')}.svg";

        private string OrganizationBadgeUrl => $"api/v1/reporting/donororganization/{_context.DonorOrganizationId}/badge.svg";

        private static string ContributorBadgeUrl(string contributorId) => $"api/v1/reporting/contributor/{contributorId}/badge.svg";

        // Arrange

        [Given("The donation data is unavailable")]
        public void GivenTheDonationDataIsUnavailable()
        {
            _donationData.MakeUnavailable();
        }

        // Act

        [When("^The user requests the (hours received|progress) badge of the project$")]
        public Task WhenTheUserRequestsTheBadgeOfTheProjectAsync(string variant)
        {
            return _context.GetAsync(_httpClient, ProjectBadgeUrl(variant));
        }

        [When("The user requests the badge of the donor organization")]
        public Task WhenTheUserRequestsTheBadgeOfTheDonorOrganizationAsync()
        {
            return _context.GetAsync(_httpClient, OrganizationBadgeUrl);
        }

        [When("The user requests the badge of Contributor {string}")]
        public Task WhenTheUserRequestsTheBadgeOfContributorAsync(string login)
        {
            return _context.GetAsync(_httpClient, ContributorBadgeUrl(Constants.ContributorIdOf(login)));
        }

        [When("The user requests the badge of the unknown contributor")]
        public Task WhenTheUserRequestsTheBadgeOfTheUnknownContributorAsync()
        {
            return _context.GetAsync(_httpClient, ContributorBadgeUrl(Constants.ContributorIdOf("an-unknown-contributor")));
        }

        [When("The badge of the project is requested by more visitors than the public limit allows")]
        public async Task WhenTheBadgeOfTheProjectIsRequestedByMoreVisitorsThanTheLimitAllowsAsync()
        {
            const int limit = 3;
            _publicRateLimit.Lower(limit);

            // The last answer is kept for the Then-steps: the first request over the limit.
            for (int i = 0; i <= limit; i++)
            {
                await _context.GetAsync(_httpClient, ProjectBadgeUrl("hours received"));
            }
        }

        // Assert

        [Then("The badge shows {string}")]
        public void ThenTheBadgeShows(string value)
        {
            _context.LastStatusCode.Should().Be(HttpStatusCode.OK, "a badge is always an image, also when there is nothing to show");
            _context.LastContentType.Should().Be("image/svg+xml");

            // Parsing proves the image is well-formed, so the text of the badge is escaped.
            XElement svg = XDocument.Parse(_context.LastBody!).Root!;
            svg.Name.LocalName.Should().Be("svg");
            svg.Attribute("aria-label")!.Value.Should().Be($"{Label}: {value}");

            // The label is the brand lockup (drawn as outlines), not a text element.
            svg.Descendants().Should().Contain(e => e.Attribute("data-logo") != null && e.Attribute("data-logo")!.Value == "lockup");

            List<string> texts = svg.Descendants().Where(e => e.Name.LocalName == "text").Select(e => e.Value).ToList();
            texts.Should().Contain(value);
        }

        [Then("The badge is identical to the badge of Contributor {string}")]
        public async Task ThenTheBadgeIsIdenticalToTheBadgeOfContributorAsync(string login)
        {
            _context.LastContentType.Should().Be("image/svg+xml");
            string unknownBadge = _context.LastBody!;
            string? unknownCacheControl = _context.LastCacheControl;

            await _context.GetAsync(_httpClient, ContributorBadgeUrl(Constants.ContributorIdOf(login)));

            _context.LastBody.Should().Be(unknownBadge);
            _context.LastCacheControl.Should().Be(unknownCacheControl);
        }

        [Then("^The badge may be cached by others (for a limited time|only for a short time)$")]
        public void ThenTheBadgeMayBeCachedByOthers(string duration)
        {
            int seconds = duration == "for a limited time" ? LimitedCacheSeconds : ShortCacheSeconds;

            _context.LastCacheControl.Should().Be($"public, max-age={seconds}");
        }

        [Then("The badge does not expose contributor {string}")]
        public void ThenTheBadgeDoesNotExposeContributor(string login)
        {
            _context.LastContentType.Should().Be("image/svg+xml");
            _context.LastBody.Should().NotContain(login).And.NotContain(Constants.ContributorIdOf(login));
        }
    }
}