namespace TimeForCode.Donation.Domain
{
    public class DonationTransaction
    {
        public Donation Donation { get; private init; }
        public Contributor Contributor { get; private init; }
        public int Hours { get; private init; }
        private DonationTransaction(Donation donation, Contributor contributor, int hours)
        {
            Donation = donation;
            Contributor = contributor;
            Hours = hours;
        }

        public static DonationTransaction Create(Donation donation, Contributor contributor, int hours)
        {
            if (donation == null)
            {
                throw new ArgumentNullException(nameof(donation), "Donor cannot be null.");
            }

            if (contributor == null)
            {
                throw new ArgumentNullException(nameof(contributor), "Contributor cannot be null.");
            }

            if (hours <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(hours), "Hours must be greater than zero.");
            }

            return new DonationTransaction(donation, contributor, hours);
        }
    }
}
