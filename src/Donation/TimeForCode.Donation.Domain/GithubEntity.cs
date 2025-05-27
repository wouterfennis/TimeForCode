namespace TimeForCode.Donation.Domain
{
    public class GithubEntity
    {
        public Uri GithubReference { get; private init; }

        protected GithubEntity(Uri githubReference)
        {
            if (githubReference == null)
            {
                throw new ArgumentNullException(nameof(githubReference), "GitHub reference cannot be null.");
            }
            GithubReference = githubReference;
        }
    }
}
