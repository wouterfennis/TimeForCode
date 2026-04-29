namespace TimeForCode.Donation.Domain
{
    public class Contributor : User
    {
        public string? Email { get; private init; }
        public string? AvatarUrl { get; private init; }
        private Contributor(string name, Uri githubReference, string? email, string? avatarUrl)
            : base(name, githubReference)
        {
            Email = email;
            AvatarUrl = avatarUrl;
        }
        public static Contributor Create(string name, Uri githubReference, string? email = null, string? avatarUrl = null)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Contributor name cannot be null or empty.", nameof(name));
            }
            if (githubReference == null)
            {
                throw new ArgumentNullException(nameof(githubReference), "GitHub reference cannot be null.");
            }
            return new Contributor(name, githubReference, email, avatarUrl);
        }

    }
}