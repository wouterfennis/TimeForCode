using TimeForCode.Donation.Application.Interfaces;
using TimeForCode.Donation.Domain;

namespace TimeForCode.Donation.Specifications.Mocking
{
    /// <summary>
    /// In-memory stand-in for the maintainer collection; mirrors the upsert-by-platform-user-id semantics of the Mongo repository.
    /// </summary>
    internal class InMemoryMaintainerRepository : IMaintainerRepository
    {
        private readonly List<Maintainer> _maintainers = [];

        internal IReadOnlyList<Maintainer> Maintainers => _maintainers;

        internal void Clear()
        {
            _maintainers.Clear();
        }

        internal void Add(Maintainer maintainer)
        {
            _maintainers.Add(maintainer);
        }

        public Task<Maintainer> UpsertAsync(Maintainer maintainer)
        {
            var existing = _maintainers.SingleOrDefault(m => m.PlatformUserId == maintainer.PlatformUserId);
            if (existing != null)
            {
                _maintainers.Remove(existing);
            }

            var persisted = new Maintainer
            {
                Id = existing?.Id ?? maintainer.Id,
                PlatformUserId = maintainer.PlatformUserId,
                GithubLogin = maintainer.GithubLogin,
                AvatarUrl = maintainer.AvatarUrl
            };
            _maintainers.Add(persisted);

            return Task.FromResult(persisted);
        }

        public Task<Maintainer?> GetByPlatformUserIdAsync(string platformUserId)
        {
            return Task.FromResult(_maintainers.SingleOrDefault(m => m.PlatformUserId == platformUserId));
        }
    }
}