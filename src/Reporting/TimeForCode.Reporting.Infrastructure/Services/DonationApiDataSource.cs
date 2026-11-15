using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http.Headers;
using TimeForCode.Donation.Api.Client;
using TimeForCode.Reporting.Application.Interfaces;
using TimeForCode.Reporting.Commands;
using TimeForCode.Reporting.Domain;
using ClientDonationStatus = TimeForCode.Donation.Api.Client.DonationStatus;
using DonationStatus = TimeForCode.Reporting.Values.DonationStatus;

namespace TimeForCode.Reporting.Infrastructure.Services
{
    /// <summary>
    /// Reads the donations and contributions of a donor organization from the Donation API, with the caller's own access token.
    /// The Donation API decides who may read: its unauthorized, forbidden and not found answers are passed on; any other failure,
    /// and an unreachable Donation API, becomes a bad gateway.
    /// </summary>
    internal class DonationApiDataSource : IDonationDataSource
    {
        /// <summary>Name of the configured <see cref="HttpClient"/> that points at the Donation API.</summary>
        internal const string HttpClientName = "DonationApi";

        /// <summary>Largest page the Donation API serves.</summary>
        private const int PageSize = 100;

        /// <summary>Upper bound on the requests for one list (100000 items), so a runaway total cannot keep a request busy.</summary>
        private const int MaxPages = 1000;

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<DonationApiDataSource> _logger;

        public DonationApiDataSource(IHttpClientFactory httpClientFactory, ILogger<DonationApiDataSource> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public Task<Result<IReadOnlyList<OrganizationDonation>>> GetDonationsAsync(string donorOrganizationId, string accessToken, CancellationToken cancellationToken)
        {
            return ReadAsync(
                accessToken,
                client => ReadAllPagesAsync(async pageNumber =>
                {
                    GetDonorOrganizationDonationsResponse response = await client.GetDonorOrganizationDonationsAsync(donorOrganizationId, pageNumber, PageSize, cancellationToken);
                    return (
                        response.Donations.Select(d => new OrganizationDonation(d.ProjectId, ToStatus(d.Status), d.HoursCommitted, d.HoursLogged)).ToList(),
                        response.TotalCount);
                }),
                "donations",
                $"donor organization {donorOrganizationId}",
                OrganizationRefusal);
        }

        public Task<Result<IReadOnlyList<OrganizationContribution>>> GetContributionsAsync(string donorOrganizationId, string accessToken, CancellationToken cancellationToken)
        {
            return ReadAsync(
                accessToken,
                client => ReadAllPagesAsync(async pageNumber =>
                {
                    GetDonorOrganizationContributionsResponse response = await client.GetDonorOrganizationContributionsAsync(donorOrganizationId, pageNumber, PageSize, cancellationToken);
                    return (
                        response.Contributions.Select(c => new OrganizationContribution(c.Id, c.ProjectId, c.ContributorGitHubLogin, c.Hours, c.GithubReference, c.LoggedAt)).ToList(),
                        response.TotalCount);
                }),
                "contributions",
                $"donor organization {donorOrganizationId}",
                OrganizationRefusal);
        }

        public Task<Result<bool>> VerifyMaintainerAsync(string projectId, string accessToken, CancellationToken cancellationToken)
        {
            return ReadAsync(
                accessToken,
                async client =>
                {
                    await client.GetMaintainedProjectAsync(projectId, cancellationToken);
                    return true;
                },
                "maintainer check",
                $"project {projectId}",
                ProjectRefusal);
        }

        public Task<Result<IReadOnlyList<ProjectDonation>>> GetProjectDonationsAsync(string projectId, CancellationToken cancellationToken)
        {
            return ReadAsync<IReadOnlyList<ProjectDonation>>(
                null,
                async client =>
                {
                    GetDonationsResponse response = await client.GetProjectDonationsAsync(projectId, cancellationToken);
                    return response.Donations
                        .Select(d => new ProjectDonation(d.Id, d.DonorOrganizationId, ToStatus(d.Status), d.HoursCommitted, d.HoursLogged, d.CreatedAt))
                        .ToList();
                },
                "donations",
                $"project {projectId}",
                ProjectRefusal);
        }

        public Task<Result<IReadOnlyList<ProjectContribution>>> GetProjectContributionsAsync(string projectId, CancellationToken cancellationToken)
        {
            return ReadAsync<IReadOnlyList<ProjectContribution>>(
                null,
                async client =>
                {
                    GetContributionsResponse response = await client.GetContributionsAsync(projectId, cancellationToken);
                    return response.Contributions
                        .Select(c => new ProjectContribution(c.ContributorId, c.ContributorGitHubLogin, c.Hours))
                        .ToList();
                },
                "contributions",
                $"project {projectId}",
                ProjectRefusal);
        }

        public Task<Result<int>> GetOrganizationHoursAsync(string donorOrganizationId, CancellationToken cancellationToken)
        {
            return ReadAsync<int>(
                null,
                async client => (await client.GetDonorOrganizationHoursAsync(donorOrganizationId, cancellationToken)).HoursLogged,
                "hours",
                $"donor organization {donorOrganizationId}",
                _ => "Donor organization not found.");
        }

        public Task<Result<int>> GetContributorHoursAsync(string contributorId, CancellationToken cancellationToken)
        {
            return ReadAsync<int>(
                null,
                async client => (await client.GetContributorHoursAsync(contributorId, cancellationToken)).TotalHours,
                "hours",
                $"contributor {contributorId}",
                _ => "Contributor not found.");
        }

        public Task<Result<PlatformStatistics>> GetPlatformStatisticsAsync(CancellationToken cancellationToken)
        {
            return ReadAsync<PlatformStatistics>(
                null,
                async client =>
                {
                    PlatformStatisticsResponse response = await client.GetPlatformStatisticsAsync(cancellationToken);
                    return new PlatformStatistics(response.TotalHoursLogged, response.OrganizationsSupported);
                },
                "statistics",
                "the platform",
                _ => "The Donation service could not provide the statistics.");
        }

        /// <summary>
        /// Reads page after page until the total the Donation API reports is collected. An empty page ends the loop as well, so a
        /// total that shrinks while reading cannot keep it running. Paging is by offset, so rows added or deleted while reading can
        /// shift the pages; the report is a snapshot of a moving list. At most <see cref="MaxPages"/> pages are read.
        /// </summary>
        private static async Task<IReadOnlyList<T>> ReadAllPagesAsync<T>(Func<int, Task<(List<T> Items, int TotalCount)>> readPage)
        {
            var all = new List<T>();
            for (var pageNumber = 1; pageNumber <= MaxPages; pageNumber++)
            {
                var (items, totalCount) = await readPage(pageNumber);
                all.AddRange(items);
                if (items.Count == 0 || all.Count >= totalCount)
                {
                    return all;
                }
            }

            throw new HttpRequestException($"The Donation API holds more than {MaxPages * PageSize} items; a report cannot be built from them.");
        }

        /// <summary>
        /// Calls the Donation API, with the caller's token when there is one. <paramref name="refusalMessage"/> gives the fixed text for a
        /// passed-on 401, 403 or 404: the generated client's own message embeds the start of the downstream response, which must not reach our callers.
        /// </summary>
        private async Task<Result<T>> ReadAsync<T>(
            string? accessToken,
            Func<DonationClient, Task<T>> read,
            string what,
            string subject,
            Func<int, string> refusalMessage)
        {
            using HttpClient httpClient = _httpClientFactory.CreateClient(HttpClientName);
            if (accessToken is not null)
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            }

            try
            {
                return Result<T>.Success(await read(new DonationClient(httpClient)));
            }
            catch (ApiException exception) when (exception.StatusCode is 401 or 403 or 404)
            {
                _logger.LogWarning(exception, "The Donation API refused the {What} of {Subject} with status {StatusCode}", what, subject, exception.StatusCode);
                return Result<T>.Failure(refusalMessage(exception.StatusCode), (HttpStatusCode)exception.StatusCode);
            }
            catch (ApiException exception)
            {
                _logger.LogError(exception, "The Donation API failed on the {What} of {Subject} with status {StatusCode}", what, subject, exception.StatusCode);
                return Result<T>.Failure("The Donation service could not provide the data.", HttpStatusCode.BadGateway);
            }
            catch (HttpRequestException exception)
            {
                _logger.LogError(exception, "The Donation API could not be reached for the {What} of {Subject}", what, subject);
                return Result<T>.Failure("The Donation service could not be reached.", HttpStatusCode.BadGateway);
            }
        }

        private static string OrganizationRefusal(int statusCode) => statusCode switch
        {
            401 => "The Donation service did not accept the access token.",
            403 => "Only members of the donor organization or an administrator can read its data.",
            404 => "Donor organization not found.",
            _ => throw new ArgumentOutOfRangeException(nameof(statusCode), statusCode, "Only 401, 403 and 404 are passed on.")
        };

        private static string ProjectRefusal(int statusCode) => statusCode switch
        {
            401 => "The Donation service did not accept the access token.",
            403 => "Only the maintainer of the project or an administrator can read its donation report.",
            404 => "Project not found.",
            _ => throw new ArgumentOutOfRangeException(nameof(statusCode), statusCode, "Only 401, 403 and 404 are passed on.")
        };

        private static DonationStatus ToStatus(ClientDonationStatus status) => status switch
        {
            ClientDonationStatus.Pending => DonationStatus.Pending,
            ClientDonationStatus.Active => DonationStatus.Active,
            ClientDonationStatus.Completed => DonationStatus.Completed,
            ClientDonationStatus.Cancelled => DonationStatus.Cancelled,
            _ => throw new InvalidOperationException($"Unknown donation status {status}.")
        };
    }
}