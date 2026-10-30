using TimeForCode.Donation.Api.Client;
using TimeForCode.Donation.Domain;

namespace TimeForCode.Donation.Specifications.Steps
{
    /// <summary>
    /// State shared by the step classes of one scenario: the failed API response, the results several aggregates assert on and the ids of created entities.
    /// Reqnroll injects one instance per scenario.
    /// </summary>
    internal class DonationContext
    {
        /// <summary>The failed API response of the last call, asserted by the rejection steps.</summary>
        public ApiException? LastException { get; set; }

        public string? RegisteredProjectId { get; set; }

        public string? DonorOrganizationId { get; set; }

        /// <summary>The contribution the correct and delete steps act on: the last one logged, or an unknown identifier.</summary>
        public string? ContributionId { get; set; }

        /// <summary>The project the lifecycle, maintainer and search steps work on.</summary>
        public Project? LifecycleProject { get; set; }

        public List<Project> Catalog { get; } = [];

        /// <summary>Every project of the scenario, so the own-projects listing is answered like the real repository: by publisher.</summary>
        public List<Project> KnownProjects { get; } = [];

        public GetProjectsResponse? ProjectsResult { get; set; }

        public GetMyProjectsResult? MyProjects { get; set; }

        public GetContributionsResponse? ContributionsResult { get; set; }

        public GetDonationsResponse? DonationsResult { get; set; }

        /// <summary>Runs an API call and keeps a failed response in <see cref="LastException"/> for the Then-steps.</summary>
        public async Task CaptureAsync(Func<Task> apiCall)
        {
            try
            {
                await apiCall();
            }
            catch (ApiException exception)
            {
                LastException = exception;
            }
        }
    }
}