using AwesomeAssertions;
using TimeForCode.Donation.Application.Tests.Handlers;
using TimeForCode.Donation.Domain;
using TimeForCode.Donation.Values;

namespace TimeForCode.Donation.Application.Tests.Domain
{
    [TestClass]
    public class DonorOrganizationTests
    {
        private static readonly DateTimeOffset Now = new(2026, 11, 21, 10, 0, 0, TimeSpan.Zero);

        [TestMethod]
        public void Create_WithTime_SetsCreatedAtToThatTime()
        {
            var organization = DonorOrganization.Create("Acme", null, null, Now);

            organization.CreatedAt.Should().Be(Now);
            organization.UpdatedAt.Should().BeNull();
        }

        [TestMethod]
        public void Update_WithTime_ChangesDetailsAndSetsUpdatedAtToThatTime()
        {
            var organization = DonorOrganization.Create("Acme", null, null, Now);

            organization.Update("Acme Inc", "info@acme.org", "https://acme.org", Now.AddHours(1));

            organization.Name.Should().Be("Acme Inc");
            organization.ContactEmail.Should().Be("info@acme.org");
            organization.Website.Should().Be("https://acme.org");
            organization.UpdatedAt.Should().Be(Now.AddHours(1));
        }

        [TestMethod]
        public void HoursSpent_DonationsOfSeveralOrganizations_SumsOnlyOwnLoggedHours()
        {
            var organization = DonorOrganization.Create("Acme", null, null, DonationTestData.Now);
            var other = DonorOrganization.Create("Other", null, null, DonationTestData.Now);
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
            var organization = DonorOrganization.Create("Acme", null, null, DonationTestData.Now);
            var other = DonorOrganization.Create("Other", null, null, DonationTestData.Now);
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