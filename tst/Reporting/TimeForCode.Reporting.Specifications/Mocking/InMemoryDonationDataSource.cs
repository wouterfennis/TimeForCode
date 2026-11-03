using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using TimeForCode.Reporting.Application.Interfaces;
using TimeForCode.Reporting.Commands;
using TimeForCode.Reporting.Domain;
using TimeForCode.Reporting.Values;

namespace TimeForCode.Reporting.Specifications.Mocking
{
    /// <summary>
    /// In-memory stand-in for the Donation context. Like the real one it answers not found for an unknown organization and
    /// forbidden unless the caller of the access token is a member of the organization or an administrator.
    /// </summary>
    internal class InMemoryDonationDataSource : IDonationDataSource
    {
        private readonly Dictionary<string, Organization> _organizations = [];
        private readonly Dictionary<string, Project> _projects = [];

        internal void Clear()
        {
            _organizations.Clear();
            _projects.Clear();
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

        internal void AddProject(string id, string? maintainerUserId = null)
        {
            if (_projects.TryGetValue(id, out Project? existing))
            {
                existing.MaintainerUserId ??= maintainerUserId;
                return;
            }

            _projects[id] = new Project { MaintainerUserId = maintainerUserId };
        }

        internal void AddProjectDonation(string id, ProjectDonation donation)
        {
            _projects[id].Donations.Add(donation);
        }

        internal void AddProjectContribution(string id, ProjectContribution contribution)
        {
            _projects[id].Contributions.Add(contribution);
        }

        /// <summary>Counts the hours logged on the latest Active donation to the project, like the Donation context does when hours are logged; nothing counts without one.</summary>
        internal void AddProjectLoggedHours(string id, int hours)
        {
            List<ProjectDonation> donations = _projects[id].Donations;
            int index = donations.FindLastIndex(d => d.Status == DonationStatus.Active);
            if (index >= 0)
            {
                donations[index] = donations[index] with { HoursLogged = donations[index].HoursLogged + hours };
            }
        }

        /// <summary>Like the Donation context's maintainer read: not found for an unknown project, forbidden unless the caller published it.</summary>
        public Task<Result<bool>> VerifyMaintainerAsync(string projectId, string accessToken, CancellationToken cancellationToken)
        {
            if (!_projects.TryGetValue(projectId, out Project? project))
            {
                return Task.FromResult(Result<bool>.Failure("Project not found.", HttpStatusCode.NotFound));
            }

            (_, string? userId) = ReadToken(accessToken);
            if (userId == null || userId != project.MaintainerUserId)
            {
                return Task.FromResult(Result<bool>.Failure("Only the maintainer of the project or an administrator can read its donation report.", HttpStatusCode.Forbidden));
            }

            return Task.FromResult(Result<bool>.Success(true));
        }

        public Task<Result<IReadOnlyList<ProjectDonation>>> GetProjectDonationsAsync(string projectId, CancellationToken cancellationToken)
        {
            return Task.FromResult(ReadProject(projectId, p => p.Donations));
        }

        public Task<Result<IReadOnlyList<ProjectContribution>>> GetProjectContributionsAsync(string projectId, CancellationToken cancellationToken)
        {
            return Task.FromResult(ReadProject(projectId, p => p.Contributions));
        }

        private Result<IReadOnlyList<T>> ReadProject<T>(string projectId, Func<Project, List<T>> select)
        {
            return _projects.TryGetValue(projectId, out Project? project)
                ? Result<IReadOnlyList<T>>.Success(select(project).ToList())
                : Result<IReadOnlyList<T>>.Failure("Project not found.", HttpStatusCode.NotFound);
        }

        private static (bool IsAdministrator, string? UserId) ReadToken(string accessToken)
        {
            JwtSecurityToken token = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);
            bool isAdministrator = token.Claims.Any(c => c.Type == "scope" && c.Value == "admin");
            string? userId = token.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier || c.Type == JwtRegisteredClaimNames.Sub)?.Value;
            return (isAdministrator, userId);
        }

        private Result<IReadOnlyList<T>> Read<T>(string donorOrganizationId, string accessToken, Func<Organization, List<T>> select)
        {
            if (!_organizations.TryGetValue(donorOrganizationId, out Organization? organization))
            {
                return Result<IReadOnlyList<T>>.Failure("Donor organization not found.", HttpStatusCode.NotFound);
            }

            (bool isAdministrator, string? userId) = ReadToken(accessToken);

            if (!isAdministrator && (userId == null || !organization.Members.Contains(userId)))
            {
                return Result<IReadOnlyList<T>>.Failure("Only members of the donor organization or an administrator can read its data.", HttpStatusCode.Forbidden);
            }

            return Result<IReadOnlyList<T>>.Success(select(organization).ToList());
        }

        private sealed class Project
        {
            internal string? MaintainerUserId { get; set; }
            internal List<ProjectDonation> Donations { get; } = [];
            internal List<ProjectContribution> Contributions { get; } = [];
        }

        private sealed class Organization
        {
            internal HashSet<string> Members { get; } = [];
            internal List<OrganizationDonation> Donations { get; } = [];
            internal List<OrganizationContribution> Contributions { get; } = [];
        }
    }
}