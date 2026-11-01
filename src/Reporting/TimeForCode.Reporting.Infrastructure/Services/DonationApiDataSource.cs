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
                async client =>
                {
                    GetDonationsResponse response = await client.GetDonorOrganizationDonationsAsync(donorOrganizationId, cancellationToken);
                    return (IReadOnlyList<OrganizationDonation>)response.Donations
                        .Select(d => new OrganizationDonation(d.ProjectId, ToStatus(d.Status), d.HoursCommitted, d.HoursLogged))
                        .ToList();
                },
                "donations",
                donorOrganizationId);
        }

        public Task<Result<IReadOnlyList<OrganizationContribution>>> GetContributionsAsync(string donorOrganizationId, string accessToken, CancellationToken cancellationToken)
        {
            return ReadAsync(
                accessToken,
                async client =>
                {
                    GetContributionsResponse response = await client.GetDonorOrganizationContributionsAsync(donorOrganizationId, cancellationToken);
                    return (IReadOnlyList<OrganizationContribution>)response.Contributions
                        .Select(c => new OrganizationContribution(c.Id, c.ProjectId, c.ContributorGitHubLogin, c.Hours, c.GithubReference, c.LoggedAt))
                        .ToList();
                },
                "contributions",
                donorOrganizationId);
        }

        private async Task<Result<IReadOnlyList<T>>> ReadAsync<T>(
            string accessToken,
            Func<DonationClient, Task<IReadOnlyList<T>>> read,
            string what,
            string donorOrganizationId)
        {
            using HttpClient httpClient = _httpClientFactory.CreateClient(HttpClientName);
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            try
            {
                return Result<IReadOnlyList<T>>.Success(await read(new DonationClient(httpClient)));
            }
            catch (ApiException exception) when (exception.StatusCode is 401 or 403 or 404)
            {
                _logger.LogWarning(exception, "The Donation API refused the {What} of donor organization {DonorOrganizationId} with status {StatusCode}", what, donorOrganizationId, exception.StatusCode);
                return Result<IReadOnlyList<T>>.Failure(RefusalMessage(exception.StatusCode), (HttpStatusCode)exception.StatusCode);
            }
            catch (ApiException exception)
            {
                _logger.LogError(exception, "The Donation API failed on the {What} of donor organization {DonorOrganizationId} with status {StatusCode}", what, donorOrganizationId, exception.StatusCode);
                return Result<IReadOnlyList<T>>.Failure("The Donation service could not provide the data.", HttpStatusCode.BadGateway);
            }
            catch (HttpRequestException exception)
            {
                _logger.LogError(exception, "The Donation API could not be reached for the {What} of donor organization {DonorOrganizationId}", what, donorOrganizationId);
                return Result<IReadOnlyList<T>>.Failure("The Donation service could not be reached.", HttpStatusCode.BadGateway);
            }
        }

        /// <summary>
        /// A fixed message per status: the generated client's own message embeds the start of the downstream response, which must not reach our callers.
        /// </summary>
        private static string RefusalMessage(int statusCode) => statusCode switch
        {
            401 => "The Donation service did not accept the access token.",
            403 => "Only members of the donor organization or an administrator can read its data.",
            404 => "Donor organization not found.",
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