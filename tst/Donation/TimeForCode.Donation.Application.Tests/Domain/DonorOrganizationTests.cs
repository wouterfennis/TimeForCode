using AwesomeAssertions;
using TimeForCode.Donation.Application.Tests.Handlers;
using TimeForCode.Donation.Domain;
using TimeForCode.Donation.Values;

namespace TimeForCode.Donation.Application.Tests.Domain
{
    [TestClass]
    public class DonorOrganizationTests
    {
        [TestMethod]
        public void HoursSpent_DonationsOfSeveralOrganizations_SumsOnlyOwnLoggedHours()
        {
            var organization = DonorOrganization.Create("Acme", null, null);
            var other = DonorOrganization.Create("Other", null, null);
            var donations = new[]
            {
                DonationTestData.Build(DonationStatus.Active, committed: 10, logged: 4, organizationId: organization.Id),
                DonationTestData.Build(DonationStatus.Completed, committed: 7, organizationId: organization.Id),
                DonationTestData.Build(DonationStatus.Active, committed: 10, logged: 3, organizationId: other.Id)
            };

            organization.HoursSpent(donations).Should().Be(11);
        }

        [TestMethod]
        public void HoursAvailable_DonationsInVariousStates_CountsOnlyRemainingHoursOfActiveOwnDonations()
        {
            var organization = DonorOrganization.Create("Acme", null, null);
            var other = DonorOrganization.Create("Other", null, null);
            var donations = new[]
            {
                DonationTestData.Build(DonationStatus.Active, committed: 10, logged: 4, organizationId: organization.Id),
                DonationTestData.Build(DonationStatus.Pending, committed: 5, organizationId: organization.Id),
                DonationTestData.Build(DonationStatus.Completed, committed: 7, organizationId: organization.Id),
                DonationTestData.Build(DonationStatus.Active, committed: 10, organizationId: other.Id)
            };

            organization.HoursAvailable(donations).Should().Be(6);
        }
    }
}