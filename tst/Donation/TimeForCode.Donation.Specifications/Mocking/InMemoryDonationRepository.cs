using MongoDB.Bson;
using TimeForCode.Donation.Application.Interfaces;
using TimeForCode.Donation.Values;
using DonationEntity = TimeForCode.Donation.Domain.Donation;

namespace TimeForCode.Donation.Specifications.Mocking
{
    /// <summary>
    /// In-memory stand-in for the donation collection; lists donations oldest first, like the real repository.
    /// </summary>
    internal class InMemoryDonationRepository : IDonationRepository
    {
        private readonly List<DonationEntity> _donations = [];

        internal void Clear()
        {
            _donations.Clear();
        }

        internal IReadOnlyList<DonationEntity> Donations => _donations;

        public Task CreateAsync(DonationEntity donation)
        {
            _donations.Add(donation);
            return Task.CompletedTask;
        }

        public Task<DonationEntity?> GetByIdAsync(string id)
        {
            return Task.FromResult(_donations.FirstOrDefault(d => d.Id.ToString() == id));
        }

        public Task UpdateAsync(DonationEntity donation)
        {
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<DonationEntity>> GetByPledgedByUserIdAsync(string userId, CancellationToken cancellationToken)
        {
            return OldestFirst(d => d.PledgedByUserId == userId);
        }

        public Task<IReadOnlyList<DonationEntity>> GetByProjectIdAsync(string projectId, CancellationToken cancellationToken)
        {
            return OldestFirst(d => d.ProjectId.ToString() == projectId);
        }

        public Task<IReadOnlyList<DonationEntity>> GetActiveByProjectIdAsync(string projectId, CancellationToken cancellationToken)
        {
            return OldestFirst(d => d.ProjectId.ToString() == projectId && d.Status == DonationStatus.Active);
        }

        public Task<IReadOnlyList<DonationEntity>> GetReceivedByProjectIdsAsync(IReadOnlyCollection<string> projectIds, CancellationToken cancellationToken)
        {
            return OldestFirst(d => projectIds.Contains(d.ProjectId.ToString()) && d.Status is DonationStatus.Active or DonationStatus.Completed);
        }

        public Task<IReadOnlyList<DonationEntity>> GetByDonorOrganizationIdsAsync(IReadOnlyCollection<string> organizationIds, CancellationToken cancellationToken)
        {
            return OldestFirst(d => d.DonorOrganizationId is ObjectId id && organizationIds.Contains(id.ToString()));
        }

        public Task<(IReadOnlyList<DonationEntity> Donations, int TotalCount)> GetPageByDonorOrganizationIdAsync(string organizationId, int pageNumber, int pageSize, CancellationToken cancellationToken)
        {
            var matches = _donations
                .Where(d => d.DonorOrganizationId is ObjectId id && id.ToString() == organizationId)
                .OrderBy(d => d.CreatedAt)
                .ThenBy(d => d.Id)
                .ToList();
            IReadOnlyList<DonationEntity> page = matches.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

            return Task.FromResult((page, matches.Count));
        }

        private Task<IReadOnlyList<DonationEntity>> OldestFirst(Func<DonationEntity, bool> predicate)
        {
            IReadOnlyList<DonationEntity> matches = _donations
                .Where(predicate)
                .OrderBy(d => d.CreatedAt)
                .ThenBy(d => d.Id)
                .ToList();

            return Task.FromResult(matches);
        }
    }
}