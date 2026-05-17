using MongoDB.Bson;
using MongoDB.Driver;
using TimeForCode.Donation.Application.Interfaces;
using TimeForCode.Donation.Domain;
using TimeForCode.Donation.Values;

namespace TimeForCode.Donation.Infrastructure.Persistence.Database
{
    internal class ProjectRepository : IProjectRepository
    {
        private readonly IMongoCollection<Project> _collection;

        public ProjectRepository(IMongoDbContext context)
        {
            _collection = context.GetCollection<Project>();
        }

        public async Task<Project?> GetByIdAsync(string id)
        {
            if (!ObjectId.TryParse(id, out var objectId))
            {
                return null;
            }

            return await _collection
                .Find(Builders<Project>.Filter.Eq("_id", objectId))
                .FirstOrDefaultAsync();
        }

        public async Task<(IReadOnlyList<Project> Projects, int TotalCount)> GetAllPublishedAsync(int pageNumber, int pageSize)
        {
            var filter = Builders<Project>.Filter.Eq(p => p.Status, ProjectStatus.Published);
            var totalCount = (int)await _collection.CountDocumentsAsync(filter);
            var projects = await _collection
                .Find(filter)
                .SortByDescending(p => p.PublishedAt)
                .ThenByDescending(p => p.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Limit(pageSize)
                .ToListAsync();

            return (projects, totalCount);
        }

        public async Task CreateAsync(Project project)
        {
            await _collection.InsertOneAsync(project);
        }

        public async Task UpdateAsync(Project project)
        {
            var filter = Builders<Project>.Filter.Eq("_id", project.Id);
            var update = Builders<Project>.Update.Set(p => p.Status, project.Status);
            await _collection.UpdateOneAsync(filter, update);
        }

        public async Task<Project?> GetByGithubUrlAsync(Uri githubRepositoryUrl)
        {
            return await _collection
                .Find(Builders<Project>.Filter.Eq(p => p.GithubRepositoryUrl, githubRepositoryUrl))
                .FirstOrDefaultAsync();
        }
    }
}
