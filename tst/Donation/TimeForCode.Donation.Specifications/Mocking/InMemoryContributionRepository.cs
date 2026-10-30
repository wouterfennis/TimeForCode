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

        internal void Clear()
        {
            _contributions.Clear();
        }

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
                    LoggedAt = c.LoggedAt,
                    History = c.History,
                    DeletedAt = c.DeletedAt,
                    DeletedByUserId = c.DeletedByUserId
                };
            }

            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<Contribution>> GetByContributorIdsAsync(IReadOnlyCollection<ObjectId> contributorIds, CancellationToken cancellationToken)
        {
            IReadOnlyList<Contribution> matches = _contributions
                .Where(c => contributorIds.Contains(c.ContributorId) && !c.IsDeleted)
                .OrderBy(c => c.LoggedAt)
                .ToList();
            return Task.FromResult(matches);
        }

        public Task<IReadOnlyList<Contribution>> GetByProjectIdAsync(string projectId, CancellationToken cancellationToken)
        {
            IReadOnlyList<Contribution> matches = _contributions
                .Where(c => c.ProjectId.ToString() == projectId && !c.IsDeleted)
                .OrderBy(c => c.LoggedAt)
                .ThenBy(c => c.Id)
                .ToList();

            return Task.FromResult(matches);
        }

        public Task<Contribution?> GetByIdAsync(string projectId, string contributionId, CancellationToken cancellationToken)
        {
            Contribution? match = _contributions.SingleOrDefault(c =>
                c.Id.ToString() == contributionId && c.ProjectId.ToString() == projectId && !c.IsDeleted);

            return Task.FromResult(match);
        }

        public Task UpdateAsync(Contribution contribution, CancellationToken cancellationToken)
        {
            _contributions[_contributions.FindIndex(c => c.Id == contribution.Id)] = contribution;
            return Task.CompletedTask;
        }
    }
}