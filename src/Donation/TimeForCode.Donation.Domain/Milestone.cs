namespace TimeForCode.Donation.Domain
{
    public class Milestone : GithubEntity
    {
        public string Name { get; private init; }
        public int HoursNeeded { get; private init; }

        public MilestoneState Status { get; set; }

        private Milestone(string name, int hours, Uri githubReference) : base(githubReference)
        {
            Name = name;
            HoursNeeded = hours;
        }

        public static Milestone Create(string name, int hours, Uri githubReference)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Milestone name cannot be null or empty.", nameof(name));
            }
            if (hours <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(hours), "HoursNeeded must be greater than zero.");
            }
            if (githubReference == null)
            {
                throw new ArgumentNullException(nameof(githubReference), "GitHub reference cannot be null.");
            }
            return new Milestone(name, hours, githubReference);
        }
    }
}