using TimeForCode.Donation.Domain;
using TimeForCode.Donation.Values;

namespace TimeForCode.Donation.Application.Tests.Handlers
{
    internal static class ProjectTestData
    {
        internal static Project Build(ProjectStatus status, string ownerId)
        {
            return new Project
            {
                Id = new MongoDB.Bson.ObjectId("5f43a0e74b12c84f1b000001"),
                Snapshot = new GithubSnapshot
                {
                    Name = "test-repo",
                    FullName = "owner/test-repo",
                    HtmlUrl = "https://github.com/owner/test-repo",
                    Topics = [],
                    DefaultBranch = "main",
                    OwnerLogin = "owner",
                    OwnerAvatarUrl = "https://avatars.githubusercontent.com/u/1",
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow,
                    PushedAt = DateTimeOffset.UtcNow
                },
                GithubRepositoryUrl = new Uri("https://github.com/owner/test-repo"),
                Status = status,
                PublishedByUserId = ownerId,
                PublishedAt = DateTimeOffset.UtcNow
            };
        }
    }
}