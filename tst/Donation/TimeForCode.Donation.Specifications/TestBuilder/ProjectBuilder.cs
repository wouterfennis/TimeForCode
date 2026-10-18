using MongoDB.Bson;
using TimeForCode.Donation.Domain;
using TimeForCode.Donation.Values;

namespace TimeForCode.Donation.Specifications.TestBuilder
{
    internal static class ProjectBuilder
    {
        internal static Project BuildActive(string? id = null)
        {
            return Build(ProjectStatus.Active, Constants.TestUserId, id);
        }

        internal static Project BuildActive(
            int number,
            string? name = null,
            string? language = null,
            string[]? topics = null,
            DateTimeOffset? publishedAt = null)
        {
            var snapshot = BuildSnapshot();
            var customised = snapshot with
            {
                Name = name ?? snapshot.Name,
                FullName = $"testuser/{name ?? snapshot.Name}",
                Language = language ?? snapshot.Language,
                Topics = topics ?? snapshot.Topics
            };

            return Build(ProjectStatus.Active, Constants.TestUserId, IdFor(number), customised, publishedAt);
        }

        internal static string IdFor(int number)
        {
            return number.ToString("x24");
        }

        internal static Project Build(
            ProjectStatus status,
            string ownerId,
            string? id = null,
            GithubSnapshot? snapshot = null,
            DateTimeOffset? publishedAt = null)
        {
            var objectId = id != null ? new ObjectId(id) : new ObjectId(Constants.TestProjectId);
            return new Project
            {
                Id = objectId,
                Snapshot = snapshot ?? BuildSnapshot(),
                GithubRepositoryUrl = new Uri(Constants.TestGithubRepositoryUrl),
                PublishedByUserId = ownerId,
                PublishedAt = publishedAt ?? new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero)
            }.InStatus(status);
        }

        internal static GithubSnapshot BuildSnapshot()
        {
            return new GithubSnapshot
            {
                Name = "test-repo",
                FullName = "testuser/test-repo",
                Description = "A test repository",
                HtmlUrl = "https://github.com/testuser/test-repo",
                Language = "C#",
                Topics = ["dotnet", "testing"],
                StargazersCount = 42,
                ForksCount = 5,
                OpenIssuesCount = 3,
                Homepage = "https://example.com",
                DefaultBranch = "main",
                License = "MIT",
                OwnerLogin = "testuser",
                OwnerAvatarUrl = "https://avatars.githubusercontent.com/u/1?v=4",
                CreatedAt = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero),
                UpdatedAt = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero),
                PushedAt = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero),
                IsPrivate = false,
                IsArchived = false
            };
        }
    }
}