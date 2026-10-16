using TimeForCode.Donation.Application.Interfaces;
using TimeForCode.Donation.Domain;

namespace TimeForCode.Donation.Specifications.Mocking
{
    /// <summary>
    /// In-memory stand-in for the contributor collection; mirrors the upsert-by-platform-user-id semantics of the Mongo repository.
    /// </summary>
    internal class InMemoryContributorRepository : IContributorRepository
    {
        private readonly List<Contributor> _contributors = [];

        internal IReadOnlyList<Contributor> Contributors => _contributors;

        internal void Add(Contributor contributor)
        {
            _contributors.Add(contributor);
        }

        public Task<Contributor> UpsertAsync(Contributor contributor)
        {
            var existing = _contributors.SingleOrDefault(c => c.PlatformUserId == contributor.PlatformUserId);
            if (existing != null)
            {
                _contributors.Remove(existing);
            }

            var persisted = new Contributor
            {
                Id = existing?.Id ?? contributor.Id,
                PlatformUserId = contributor.PlatformUserId,
                GithubLogin = contributor.GithubLogin,
                AvatarUrl = contributor.AvatarUrl
            };
            _contributors.Add(persisted);

            return Task.FromResult(persisted);
        }
    }
}