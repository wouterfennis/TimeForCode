using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using TimeForCode.Reporting.Application.Interfaces;
using TimeForCode.Reporting.Commands;
using TimeForCode.Reporting.Domain;

namespace TimeForCode.Reporting.Specifications.Mocking
{
    /// <summary>
    /// In-memory stand-in for the Donation context. Like the real one it answers not found for an unknown organization and
    /// forbidden unless the caller of the access token is a member of the organization or an administrator.
    /// </summary>
    internal class InMemoryDonationDataSource : IDonationDataSource
    {
        private readonly Dictionary<string, Organization> _organizations = [];

        internal void Clear()
        {
            _organizations.Clear();
        }

        internal void AddOrganization(string id)
        {
            _organizations.TryAdd(id, new Organization());
        }

        internal void AddMember(string id, string userId)
        {
            _organizations[id].Members.Add(userId);
        }

        internal void AddDonation(string id, OrganizationDonation donation)
        {
            _organizations[id].Donations.Add(donation);
        }

        internal void AddContribution(string id, OrganizationContribution contribution)
        {
            _organizations[id].Contributions.Add(contribution);
        }

        /// <summary>Counts the hours logged on the donation to the project, like the Donation context does when hours are logged.</summary>
        internal void AddLoggedHours(string id, int hours)
        {
            List<OrganizationDonation> donations = _organizations[id].Donations;
            donations[^1] = donations[^1] with { HoursLogged = donations[^1].HoursLogged + hours };
        }

        public Task<Result<IReadOnlyList<OrganizationDonation>>> GetDonationsAsync(string donorOrganizationId, string accessToken, CancellationToken cancellationToken)
        {
            return Task.FromResult(Read<OrganizationDonation>(donorOrganizationId, accessToken, o => o.Donations));
        }

        public Task<Result<IReadOnlyList<OrganizationContribution>>> GetContributionsAsync(string donorOrganizationId, string accessToken, CancellationToken cancellationToken)
        {
            return Task.FromResult(Read<OrganizationContribution>(donorOrganizationId, accessToken, o => o.Contributions));
        }

        private Result<IReadOnlyList<T>> Read<T>(string donorOrganizationId, string accessToken, Func<Organization, List<T>> select)
        {
            if (!_organizations.TryGetValue(donorOrganizationId, out Organization? organization))
            {
                return Result<IReadOnlyList<T>>.Failure("Donor organization not found.", HttpStatusCode.NotFound);
            }

            JwtSecurityToken token = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);
            bool isAdministrator = token.Claims.Any(c => c.Type == "scope" && c.Value == "admin");
            string? userId = token.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier || c.Type == JwtRegisteredClaimNames.Sub)?.Value;

            if (!isAdministrator && (userId == null || !organization.Members.Contains(userId)))
            {
                return Result<IReadOnlyList<T>>.Failure("Only members of the donor organization or an administrator can read its data.", HttpStatusCode.Forbidden);
            }

            return Result<IReadOnlyList<T>>.Success(select(organization).ToList());
        }

        private sealed class Organization
        {
            internal HashSet<string> Members { get; } = [];
            internal List<OrganizationDonation> Donations { get; } = [];
            internal List<OrganizationContribution> Contributions { get; } = [];
        }
    }
}