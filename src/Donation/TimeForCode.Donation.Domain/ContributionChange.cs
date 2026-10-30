namespace TimeForCode.Donation.Domain
{
    public enum ContributionChangeFailure
    {
        None = 0,

        /// <summary>The contribution was deleted, so it can no longer be changed.</summary>
        Deleted = 1,

        /// <summary>Only the contributor and administrators may change a contribution.</summary>
        NotAllowed = 2
    }

    /// <summary>
    /// Outcome of correcting or deleting a contribution: the changed contribution and by how many hours its counted hours changed,
    /// or the reason it was refused.
    /// </summary>
    public sealed class ContributionChange
    {
        private ContributionChange(Contribution? contribution, int hoursDelta, ContributionChangeFailure failure)
        {
            Contribution = contribution;
            HoursDelta = hoursDelta;
            Failure = failure;
        }

        public bool IsSuccess => Failure == ContributionChangeFailure.None;
        public ContributionChangeFailure Failure { get; }
        public Contribution? Contribution { get; }

        /// <summary>The new hours minus the old ones: positive when hours were added, negative when hours were taken away.</summary>
        public int HoursDelta { get; }

        public static ContributionChange Success(Contribution contribution, int hoursDelta) => new(contribution, hoursDelta, ContributionChangeFailure.None);

        public static ContributionChange Refused(ContributionChangeFailure failure) => new(null, 0, failure);
    }
}