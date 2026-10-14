using TimeForCode.Donation.Commands;
using TimeForCode.Donation.Domain;

namespace TimeForCode.Donation.Application.Handlers
{
    internal static class ContributionMapper
    {
        internal static ContributionDto ToDto(this Contribution contribution)
        {
            return new ContributionDto
            {
                Id = contribution.Id.ToString(),
                ProjectId = contribution.ProjectId.ToString(),
                ContributorId = contribution.ContributorId.ToString(),
                ContributorGitHubLogin = contribution.ContributorGitHubLogin,
                ContributorAvatarUrl = contribution.ContributorAvatarUrl,
                Hours = contribution.Hours,
                GithubReference = contribution.GithubReference?.ToString(),
                LoggedAt = contribution.LoggedAt
            };
        }
    }
}