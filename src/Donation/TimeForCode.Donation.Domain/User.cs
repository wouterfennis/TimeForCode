namespace TimeForCode.Donation.Domain
{
    public class User : GithubEntity
    {
        public string Name { get; private init; }

        protected User(string name, Uri githubReference) : base(githubReference)
        {
            Name = name;
        }

        public static User Create(string name, Uri githubReference)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("User name cannot be null or empty.", nameof(name));
            }

            if (githubReference == null)
            {
                throw new ArgumentNullException(nameof(githubReference), "GitHub reference cannot be null.");
            }

            return new User(name, githubReference);
        }
    }
}
