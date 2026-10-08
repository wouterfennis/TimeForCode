using MongoDB.Bson;
using MongoDB.Driver;
using System.Diagnostics.CodeAnalysis;
using TimeForCode.Donation.Application.Exceptions;
using TimeForCode.Donation.Application.Interfaces;
using TimeForCode.Donation.Domain;
using TimeForCode.Donation.Values;

namespace TimeForCode.Donation.Infrastructure.Persistence.Database
{
    [ExcludeFromCodeCoverage(Justification = "Repository implementation")]
    internal class ProjectRepository : IProjectRepository
    {
        private static readonly object IndexCreationLock = new();
        private static bool _githubUrlIndexCreated;
        private readonly IMongoCollection<Project> _collection;

        public ProjectRepository(IMongoDbContext context)
        {
            _collection = context.GetCollection<Project>();
            EnsureGithubUrlIndex(_collection);
        }

        private static void EnsureGithubUrlIndex(IMongoCollection<Project> collection)
        {
            if (_githubUrlIndexCreated)
            {
                return;
            }

            lock (IndexCreationLock)
            {
                if (_githubUrlIndexCreated)
                {
                    return;
                }

                var indexModel = new CreateIndexModel<Project>(
                    Builders<Project>.IndexKeys.Ascending(p => p.GithubRepositoryUrl),
                    new CreateIndexOptions<Project>
                    {
                        Name = "ux_projects_github_repository_url",
                        Unique = true
                    });

                collection.Indexes.CreateOne(indexModel);
                _githubUrlIndexCreated = true;
            }
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

        public async Task<(IReadOnlyList<Project> Projects, int TotalCount)> GetAllActiveAsync(int pageNumber, int pageSize)
        {
            var filter = Builders<Project>.Filter.Eq(p => p.Status, ProjectStatus.Active);
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
            try
            {
                await _collection.InsertOneAsync(project);
            }
            catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
            {
                throw new RepositoryConflictException("Repository is already published.", ex);
            }
        }

        public async Task UpdateAsync(Project project)
        {
            var filter = Builders<Project>.Filter.Eq("_id", project.Id);
            var update = Builders<Project>.Update
                .Set(p => p.Status, project.Status)
                .Set(p => p.Snapshot, project.Snapshot)
                .Set(p => p.PublishedAt, project.PublishedAt)
                .Set(p => p.ChangeRequestReason, project.ChangeRequestReason);
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