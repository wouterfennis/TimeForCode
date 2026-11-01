using Reqnroll;
using System.Net.Http.Json;
using System.Text.Json;
using TimeForCode.Donation.Specifications.Mocking;
using TimeForCode.Reporting.Domain;
using TimeForCode.Reporting.Specifications.Mocking;
using TimeForCode.Reporting.Values;

namespace TimeForCode.Reporting.Specifications.Steps
{
    /// <summary>The donor organization, its members and what was pledged and logged for it, as the Donation context holds it.</summary>
    [Binding]
    internal class DonorOrganizationSteps
    {
        private const string ProjectId = "507f1f77bcf86cd799439012";

        private readonly InMemoryDonationDataSource _donationData;
        private readonly ReportingContext _context;
        private readonly LiveScenario _liveScenario;
        private int _contributionNumber;
        private string? _liveOrganizationId;

        public DonorOrganizationSteps(InMemoryDonationDataSource donationData, ReportingContext context, LiveScenario liveScenario)
        {
            _donationData = donationData;
            _context = context;
            _liveScenario = liveScenario;
        }

        /// <summary>A deterministic 24 character hexadecimal identifier, the shape of the identifiers the Donation context issues.</summary>
        private static string IdOf(int number) => number.ToString("x24");

        [Given("There is a donor organization on the time for code platform")]
        public async Task GivenThereIsADonorOrganizationOnTheTimeForCodePlatformAsync()
        {
            if (_liveScenario.IsLive)
            {
                _context.DonorOrganizationId = await RegisterLiveDonorOrganizationAsync();
                return;
            }

            _context.DonorOrganizationId = Constants.TestDonorOrganizationId;
            _donationData.AddOrganization(Constants.TestDonorOrganizationId);
        }

        [AfterScenario(LiveHost.Tag)]
        public async Task DeleteLiveDonorOrganizationAsync()
        {
            if (!_liveScenario.IsLive || _liveOrganizationId is null)
            {
                return;
            }

            try
            {
                using HttpClient donation = CreateDonationClient(await LiveAuthentication.GetAdminTokenAsync());
                using HttpResponseMessage response = await donation.DeleteAsync(new Uri($"api/v1/donororganization/{_liveOrganizationId}", UriKind.Relative));
                if (!response.IsSuccessStatusCode)
                {
                    // Best-effort clean-up, but a leftover organization on the live stack should not go unnoticed.
                    Console.WriteLine($"Deleting live donor organization {_liveOrganizationId} answered {(int)response.StatusCode}; it was left on the stack.");
                }
            }
            catch (HttpRequestException exception)
            {
                // Best-effort clean-up: a failure here must not hide the scenario result.
                Console.WriteLine($"Could not delete live donor organization {_liveOrganizationId}: {exception.Message}");
            }
        }

        // Registered with the Donation API directly and with its own token, so the scenario's own identity (or absence of one) stays untouched.
        private async Task<string> RegisterLiveDonorOrganizationAsync()
        {
            using HttpClient donation = CreateDonationClient(await LiveAuthentication.GetUserTokenAsync());
            using HttpResponseMessage response = await donation.PostAsJsonAsync(
                new Uri("api/v1/donororganization", UriKind.Relative),
                new { Name = $"Live Reporting Organization {Guid.NewGuid():N}" });

            response.EnsureSuccessStatusCode();
            JsonElement created = await response.Content.ReadFromJsonAsync<JsonElement>();
            _liveOrganizationId = created.GetProperty("id").GetString();
            return _liveOrganizationId!;
        }

        private static HttpClient CreateDonationClient(string token)
        {
            return LiveHost.CreateClient(LiveHost.RequiredDonationAddress, new BearerTokenHandler { Token = token });
        }

        [Given("There is no donor organization with the given identifier")]
        public void GivenThereIsNoDonorOrganizationWithTheGivenIdentifier()
        {
            _context.DonorOrganizationId = IdOf(0xdead);
        }

        [Given("The user is a member of the donor organization")]
        public async Task GivenTheUserIsAMemberOfTheDonorOrganizationAsync()
        {
            await GivenThereIsADonorOrganizationOnTheTimeForCodePlatformAsync();
            _donationData.AddMember(Constants.TestDonorOrganizationId, Constants.TestUserId);
        }

        [Given("The donor organization has an active donation of {int} hours")]
        public void GivenTheDonorOrganizationHasAnActiveDonation(int hours)
        {
            _donationData.AddOrganization(Constants.TestDonorOrganizationId);
            _donationData.AddDonation(Constants.TestDonorOrganizationId, new OrganizationDonation(ProjectId, DonationStatus.Active, hours, 0));
        }

        [Given("The donor organization has donations to {int} different projects")]
        public void GivenTheDonorOrganizationHasDonationsToDifferentProjects(int projects)
        {
            _donationData.AddOrganization(Constants.TestDonorOrganizationId);
            for (int i = 1; i <= projects; i++)
            {
                _donationData.AddDonation(Constants.TestDonorOrganizationId, new OrganizationDonation(IdOf(i), DonationStatus.Active, 10 * i, 5 * i));
            }
        }

        [Given("The donor organization has no donations")]
        public void GivenTheDonorOrganizationHasNoDonations()
        {
            _donationData.AddOrganization(Constants.TestDonorOrganizationId);
        }

        [Given("^The user has logged (\\d+) hours that count towards the donation$")]
        public void GivenTheUserHasLoggedHoursThatCountTowardsTheDonation(int hours)
        {
            _contributionNumber++;
            _donationData.AddLoggedHours(Constants.TestDonorOrganizationId, hours);
            _donationData.AddContribution(
                Constants.TestDonorOrganizationId,
                new OrganizationContribution(
                    IdOf(_contributionNumber),
                    ProjectId,
                    "octocat",
                    hours,
                    null,
                    new DateTimeOffset(2026, 10, 1, 9, 0, 0, TimeSpan.Zero).AddDays(_contributionNumber)));
        }
    }
}