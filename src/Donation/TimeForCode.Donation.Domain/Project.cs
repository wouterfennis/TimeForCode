using MongoDB.Bson;
using TimeForCode.Donation.Domain.Entities;
using TimeForCode.Donation.Values;

namespace TimeForCode.Donation.Domain
{
    public class Project : DocumentEntity
    {
        public required GithubSnapshot Snapshot { get; init; }
        public required Uri GithubRepositoryUrl { get; init; }
        public required ProjectStatus Status { get; set; }
        public required string PublishedByUserId { get; init; }
        public required DateTimeOffset PublishedAt { get; init; }

        public static Project Create(
            GithubSnapshot snapshot,
            Uri githubRepositoryUrl,
            string publishedByUserId,
            DateTimeOffset publishedAt)
        {
            return new Project
            {
                Id = ObjectId.GenerateNewId(),
                Snapshot = snapshot,
                GithubRepositoryUrl = githubRepositoryUrl,
                Status = ProjectStatus.Published,
                PublishedByUserId = publishedByUserId,
                PublishedAt = publishedAt
            };
        }
    }
}
