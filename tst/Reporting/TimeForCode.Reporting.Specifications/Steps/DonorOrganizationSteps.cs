using Reqnroll;
using TimeForCode.Reporting.Domain;
using TimeForCode.Reporting.Specifications.Mocking;
using TimeForCode.Reporting.Values;

namespace TimeForCode.Reporting.Specifications.Steps
{
    /// <summary>The donor organization, its members and what was pledged and logged for it, as the Donation context holds it.</summary>
    [Binding]
    internal class DonorOrganizationSteps
    {
        private const string ProjectId = "507f1f77bcf86cd799439012";

        private readonly InMemoryDonationDataSource _donationData;
        private readonly ReportingContext _context;
        private int _contributionNumber;

        public DonorOrganizationSteps(InMemoryDonationDataSource donationData, ReportingContext context)
        {
            _donationData = donationData;
            _context = context;
        }

        /// <summary>A deterministic 24 character hexadecimal identifier, the shape of the identifiers the Donation context issues.</summary>
        private static string IdOf(int number) => number.ToString("x24");

        [Given("There is a donor organization on the time for code platform")]
        public void GivenThereIsADonorOrganizationOnTheTimeForCodePlatform()
        {
            _context.DonorOrganizationId = Constants.TestDonorOrganizationId;
            _donationData.AddOrganization(Constants.TestDonorOrganizationId);
        }

        [Given("There is no donor organization with the given identifier")]
        public void GivenThereIsNoDonorOrganizationWithTheGivenIdentifier()
        {
            _context.DonorOrganizationId = IdOf(0xdead);
        }

        [Given("The user is a member of the donor organization")]
        public void GivenTheUserIsAMemberOfTheDonorOrganization()
        {
            GivenThereIsADonorOrganizationOnTheTimeForCodePlatform();
            _donationData.AddMember(Constants.TestDonorOrganizationId, Constants.TestUserId);
        }

        [Given("The donor organization has an active donation of {int} hours")]
        public void GivenTheDonorOrganizationHasAnActiveDonation(int hours)
        {
            _donationData.AddOrganization(Constants.TestDonorOrganizationId);
            _donationData.AddDonation(Constants.TestDonorOrganizationId, new OrganizationDonation(ProjectId, DonationStatus.Active, hours, 0));
        }

        [Given("The donor organization has donations to {int} different projects")]
        public void GivenTheDonorOrganizationHasDonationsToDifferentProjects(int projects)
        {
            _donationData.AddOrganization(Constants.TestDonorOrganizationId);
            for (int i = 1; i <= projects; i++)
            {
                _donationData.AddDonation(Constants.TestDonorOrganizationId, new OrganizationDonation(IdOf(i), DonationStatus.Active, 10 * i, 5 * i));
            }
        }

        [Given("The donor organization has no donations")]
        public void GivenTheDonorOrganizationHasNoDonations()
        {
            _donationData.AddOrganization(Constants.TestDonorOrganizationId);
        }

        [Given("^The user has logged (\\d+) hours that count towards the donation$")]
        public void GivenTheUserHasLoggedHoursThatCountTowardsTheDonation(int hours)
        {
            _contributionNumber++;
            _donationData.AddLoggedHours(Constants.TestDonorOrganizationId, hours);
            _donationData.AddContribution(
                Constants.TestDonorOrganizationId,
                new OrganizationContribution(
                    IdOf(_contributionNumber),
                    ProjectId,
                    "octocat",
                    hours,
                    null,
                    new DateTimeOffset(2026, 10, 1, 9, 0, 0, TimeSpan.Zero).AddDays(_contributionNumber)));
        }
    }
}