using Reqnroll;
using System.Net.Http.Json;
using System.Text.Json;
using TimeForCode.Reporting.Domain;
using TimeForCode.Reporting.Specifications.Mocking;
using TimeForCode.Reporting.Values;
using TimeForCode.Shared.Live;

namespace TimeForCode.Reporting.Specifications.Steps
{
    /// <summary>The project, its maintainer and what was pledged and logged for it, as the Donation context holds it.</summary>
    [Binding]
    internal class ProjectSteps
    {
        private const string MaintainerPersona = "maya-maintainer";

        private readonly InMemoryDonationDataSource _donationData;
        private readonly ReportingContext _context;
        private readonly LiveScenario _liveScenario;
        private int _donationNumber;
        private int _contributionNumber;
        private string? _liveProjectId;

        public ProjectSteps(InMemoryDonationDataSource donationData, ReportingContext context, LiveScenario liveScenario)
        {
            _donationData = donationData;
            _context = context;
            _liveScenario = liveScenario;
        }

        /// <summary>A deterministic 24 character hexadecimal identifier, the shape of the identifiers the Donation context issues.</summary>
        private static string IdOf(int number) => number.ToString("x24");

        [Given("The user is the maintainer of the project")]
        public void GivenTheUserIsTheMaintainerOfTheProject()
        {
            _context.ProjectId = Constants.TestProjectId;
            _donationData.AddProject(Constants.TestProjectId, Constants.TestUserId);
        }

        [Given("There is a project on the time for code platform")]
        public async Task GivenThereIsAProjectOnTheTimeForCodePlatformAsync()
        {
            if (_liveScenario.IsLive)
            {
                _liveProjectId = await RegisterLiveProjectAsync();
                _context.ProjectId = _liveProjectId;
                return;
            }

            _context.ProjectId = Constants.TestProjectId;
            _donationData.AddProject(Constants.TestProjectId);
        }

        [Given("There is no project with the given identifier")]
        public void GivenThereIsNoProjectWithTheGivenIdentifier()
        {
            _context.ProjectId = IdOf(0xdead);
        }

        [Given("The project has a donation of {int} hours with status {string}")]
        public void GivenTheProjectHasADonationOfHoursWithStatus(int hours, string status)
        {
            _donationNumber++;
            _donationData.AddProject(Constants.TestProjectId);
            _donationData.AddProjectDonation(
                Constants.TestProjectId,
                new ProjectDonation(
                    IdOf(0x100 + _donationNumber),
                    null,
                    Enum.Parse<DonationStatus>(status, ignoreCase: true),
                    hours,
                    0,
                    new DateTimeOffset(2026, 10, 1, 9, 0, 0, TimeSpan.Zero).AddDays(_donationNumber)));
        }

        [Given("Contributor {string} has logged {int} hours on the project")]
        public void GivenContributorHasLoggedHoursOnTheProject(string login, int hours)
        {
            _contributionNumber++;
            _donationData.AddProject(Constants.TestProjectId);
            _donationData.AddProjectLoggedHours(Constants.TestProjectId, hours);
            _donationData.AddProjectContribution(Constants.TestProjectId, new ProjectContribution($"contributor-{login}", login, hours));
        }

        // Registered, submitted and approved through the Donation API directly with its own tokens, so the scenario's own identity
        // (or absence of one) stays untouched and the scenario user is not the maintainer.
        private static async Task<string> RegisterLiveProjectAsync()
        {
            using HttpClient maintainer = CreateDonationClient(await GetMaintainerTokenAsync());

            using HttpResponseMessage registered = await maintainer.PostAsJsonAsync(
                new Uri("api/v1/project", UriKind.Relative),
                new { GithubRepositoryUrl = $"https://github.com/live-owner/live{Guid.NewGuid():N}" });
            registered.EnsureSuccessStatusCode();
            JsonElement created = await registered.Content.ReadFromJsonAsync<JsonElement>();
            string projectId = created.GetProperty("projectId").GetString()!;

            using HttpResponseMessage submitted = await maintainer.PostAsync(new Uri($"api/v1/project/{projectId}/submit", UriKind.Relative), null);
            submitted.EnsureSuccessStatusCode();

            using HttpClient administrator = CreateDonationClient(await LiveAuthentication.GetAdminTokenAsync());
            using HttpResponseMessage approved = await administrator.PostAsync(new Uri($"api/v1/project/{projectId}/approve", UriKind.Relative), null);
            approved.EnsureSuccessStatusCode();

            return projectId;
        }

        [AfterScenario(LiveHost.Tag)]
        public async Task ArchiveLiveProjectAsync()
        {
            if (!_liveScenario.IsLive || _context.ProjectId is null || _context.ProjectId != _liveProjectId)
            {
                return;
            }

            try
            {
                using HttpClient maintainer = CreateDonationClient(await GetMaintainerTokenAsync());
                using HttpResponseMessage response = await maintainer.PostAsync(new Uri($"api/v1/project/{_liveProjectId}/archive", UriKind.Relative), null);
                if (!response.IsSuccessStatusCode)
                {
                    // Best-effort clean-up, but a leftover project on the live stack should not go unnoticed.
                    Console.WriteLine($"Archiving live project {_liveProjectId} answered {(int)response.StatusCode}; it was left on the stack.");
                }
            }
            catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException)
            {
                // Best-effort clean-up: a failure here must not hide the scenario result.
                Console.WriteLine($"Could not archive live project {_liveProjectId}: {exception.Message}");
            }
        }

        private static Task<string> GetMaintainerTokenAsync()
        {
            return LiveAuthentication.SignInAsync(
                LiveAuthentication.ResolveAuthorizationAddress(Environment.GetEnvironmentVariable(LiveAuthentication.AuthorizationUrlVariable)),
                LiveAuthentication.ResolveClientRedirectUri(Environment.GetEnvironmentVariable(LiveAuthentication.ClientRedirectUriVariable)),
                MaintainerPersona);
        }

        private static HttpClient CreateDonationClient(string token)
        {
            return LiveHost.CreateClient(LiveHost.RequiredDonationAddress, new BearerTokenHandler { Token = token });
        }
    }
}