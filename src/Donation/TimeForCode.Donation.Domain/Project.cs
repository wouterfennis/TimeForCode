namespace TimeForCode.Donation.Domain
{
    public class Project
    {
        public string Name { get; private init; }
        public Maintainer Maintainer { get; private init; }
        public Uri GithubReference { get; private init; }
        public List<Milestone> Milestones { get; private init; }

        private Project(string name, Maintainer maintainer, Uri githubReference, List<Milestone> milestones)
        {
            Name = name;
            Maintainer = maintainer;
            GithubReference = githubReference;
            Milestones = milestones;
        }

        public static Project Create(string name, Maintainer maintainer, Uri githubReference, List<Milestone> milestones = null)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Project name cannot be null or empty.", nameof(name));
            }

            if (maintainer == null)
            {
                throw new ArgumentNullException(nameof(maintainer), "Maintainer cannot be null.");
            }

            if (githubReference == null)
            {
                throw new ArgumentNullException(nameof(githubReference), "GitHub reference cannot be null.");
            }

            var newMilestones = milestones ?? new List<Milestone>();

            return new Project(name, maintainer, githubReference, newMilestones);
        }
    }
}