namespace TimeForCode.Donation.Domain
{
    public class Maintainer : User
    {
        public string? Email { get; private init; }
        public string? AvatarUrl { get; private init; }
        private Maintainer(string name, Uri githubReference, string? email, string? avatarUrl)
            : base(name, githubReference)
        {
            Email = email;
            AvatarUrl = avatarUrl;
        }
        public static Maintainer Create(string name, Uri githubReference, string? email = null, string? avatarUrl = null)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Maintainer name cannot be null or empty.", nameof(name));
            }
            if (githubReference == null)
            {
                throw new ArgumentNullException(nameof(githubReference), "GitHub reference cannot be null.");
            }
            return new Maintainer(name, githubReference, email, avatarUrl);
        }
    }
}
