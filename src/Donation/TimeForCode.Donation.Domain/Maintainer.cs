using MongoDB.Bson;
using TimeForCode.Donation.Domain.Entities;

namespace TimeForCode.Donation.Domain
{
    /// <summary>
    /// A platform user who owns projects; persisted as its own document and upserted by <see cref="PlatformUserId"/>.
    /// </summary>
    public class Maintainer : DocumentEntity
    {
        public required string PlatformUserId { get; init; }
        public required string GithubLogin { get; init; }
        public required string AvatarUrl { get; init; }

        public static Maintainer Create(string platformUserId, string githubLogin, string avatarUrl)
        {
            if (string.IsNullOrWhiteSpace(platformUserId))
            {
                throw new ArgumentException("Platform user id cannot be null or empty.", nameof(platformUserId));
            }
            if (string.IsNullOrWhiteSpace(githubLogin))
            {
                throw new ArgumentException("GitHub login cannot be null or empty.", nameof(githubLogin));
            }
            if (string.IsNullOrWhiteSpace(avatarUrl))
            {
                throw new ArgumentException("Avatar URL cannot be null or empty.", nameof(avatarUrl));
            }

            return new Maintainer
            {
                Id = ObjectId.GenerateNewId(),
                PlatformUserId = platformUserId,
                GithubLogin = githubLogin,
                AvatarUrl = avatarUrl
            };
        }
    }
}