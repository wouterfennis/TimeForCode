using MongoDB.Bson;
using TimeForCode.Donation.Application.Interfaces;
using TimeForCode.Donation.Domain;

namespace TimeForCode.Donation.Specifications.Mocking
{
    /// <summary>
    /// In-memory stand-in for the contribution collection; returns the contributions of a project earliest first, like the real repository.
    /// </summary>
    internal class InMemoryContributionRepository : IContributionRepository
    {
        private readonly List<Contribution> _contributions = [];

        internal IReadOnlyList<Contribution> Contributions => _contributions;

        public Task CreateAsync(Contribution contribution)
        {
            _contributions.Add(contribution);
            return Task.CompletedTask;
        }

        public Task AnonymiseByContributorIdAsync(ObjectId contributorId, CancellationToken cancellationToken)
        {
            for (var i = 0; i < _contributions.Count; i++)
            {
                var c = _contributions[i];
                if (c.ContributorId != contributorId)
                {
                    continue;
                }

                _contributions[i] = new Contribution
                {
                    Id = c.Id,
                    ProjectId = c.ProjectId,
                    ContributorId = c.ContributorId,
                    ContributorGitHubLogin = Contributor.DeletedUserLogin,
                    ContributorAvatarUrl = Contributor.DeletedUserAvatarUrl,
                    Hours = c.Hours,
                    GithubReference = c.GithubReference,
                    LoggedAt = c.LoggedAt
                };
            }

            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<Contribution>> GetByProjectIdAsync(string projectId, CancellationToken cancellationToken)
        {
            IReadOnlyList<Contribution> matches = _contributions
                .Where(c => c.ProjectId.ToString() == projectId)
                .OrderBy(c => c.LoggedAt)
                .ThenBy(c => c.Id)
                .ToList();

            return Task.FromResult(matches);
        }
    }
}