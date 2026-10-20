using TimeForCode.Donation.Commands;
using TimeForCode.Donation.Domain;

namespace TimeForCode.Donation.Application.Handlers
{
    internal static class ContributorProfileMapper
    {
        internal static ContributorProfileDto ToProfileDto(this Contributor contributor)
        {
            return new ContributorProfileDto
            {
                Id = contributor.Id.ToString(),
                GithubLogin = contributor.GithubLogin,
                AvatarUrl = contributor.AvatarUrl,
                Role = contributor.Role,
                Skills = contributor.Skills,
                HoursAvailable = contributor.HoursAvailable,
                DonorOrganizationId = contributor.DonorOrganizationId?.ToString()
            };
        }

        /// <summary>
        /// A profile is private to its owner and to administrators.
        /// </summary>
        internal static bool MayAccess(this Contributor contributor, string callerUserId, bool isAdministrator)
        {
            return isAdministrator || contributor.PlatformUserId == callerUserId;
        }
    }
}