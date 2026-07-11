using MongoDB.Bson;
using MongoDB.Driver;
using System.Diagnostics.CodeAnalysis;
using TimeForCode.Donation.Application.Interfaces;
using TimeForCode.Donation.Domain;

namespace TimeForCode.Donation.Infrastructure.Persistence.Database
{
    [ExcludeFromCodeCoverage(Justification = "Repository implementation")]
    internal class DonorOrganizationRepository : IDonorOrganizationRepository
    {
        private static readonly object IndexCreationLock = new();
        private static bool _nameIndexCreated;
        private readonly IMongoCollection<DonorOrganization> _collection;

        public DonorOrganizationRepository(IMongoDbContext context)
        {
            _collection = context.GetCollection<DonorOrganization>();
            EnsureNameIndex(_collection);
        }

        private static void EnsureNameIndex(IMongoCollection<DonorOrganization> collection)
        {
            if (_nameIndexCreated)
            {
                return;
            }

            lock (IndexCreationLock)
            {
                if (_nameIndexCreated)
                {
                    return;
                }

                var indexModel = new CreateIndexModel<DonorOrganization>(
                    Builders<DonorOrganization>.IndexKeys.Ascending(o => o.Name),
                    new CreateIndexOptions<DonorOrganization>
                    {
                        Name = "ux_donor_organization_name",
                        Unique = true
                    });

                collection.Indexes.CreateOne(indexModel);
                _nameIndexCreated = true;
            }
        }

        public async Task<DonorOrganization?> GetByIdAsync(string id)
        {
            if (!ObjectId.TryParse(id, out var objectId))
            {
                return null;
            }

            return await _collection
                .Find(Builders<DonorOrganization>.Filter.Eq("_id", objectId))
                .FirstOrDefaultAsync();
        }

        public async Task<(IReadOnlyList<DonorOrganization> Organizations, int TotalCount)> GetAllAsync(int pageNumber, int pageSize)
        {
            var filter = Builders<DonorOrganization>.Filter.Empty;
            var totalCount = (int)await _collection.CountDocumentsAsync(filter);
            var organizations = await _collection
                .Find(filter)
                .SortBy(o => o.Name)
                .Skip((pageNumber - 1) * pageSize)
                .Limit(pageSize)
                .ToListAsync();

            return (organizations, totalCount);
        }

        public async Task CreateAsync(DonorOrganization organization)
        {
            await _collection.InsertOneAsync(organization);
        }

        public async Task UpdateAsync(DonorOrganization organization)
        {
            var filter = Builders<DonorOrganization>.Filter.Eq("_id", organization.Id);
            var update = Builders<DonorOrganization>.Update
                .Set(o => o.Name, organization.Name)
                .Set(o => o.ContactEmail, organization.ContactEmail)
                .Set(o => o.Website, organization.Website)
                .Set(o => o.UpdatedAt, organization.UpdatedAt);
            await _collection.UpdateOneAsync(filter, update);
        }

        public async Task DeleteAsync(string id)
        {
            var objectId = ObjectId.Parse(id);
            var filter = Builders<DonorOrganization>.Filter.Eq("_id", objectId);
            await _collection.DeleteOneAsync(filter);
        }

        public async Task<DonorOrganization?> GetByNameAsync(string name)
        {
            return await _collection
                .Find(Builders<DonorOrganization>.Filter.Eq(o => o.Name, name))
                .FirstOrDefaultAsync();
        }
    }
}