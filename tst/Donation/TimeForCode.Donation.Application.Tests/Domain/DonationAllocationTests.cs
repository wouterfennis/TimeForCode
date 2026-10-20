using FluentAssertions;
using MongoDB.Bson;
using TimeForCode.Donation.Domain;

namespace TimeForCode.Donation.Application.Tests.Domain
{
    [TestClass]
    public class DonationAllocationTests
    {
        private static readonly ObjectId ContributionId = new("5f43a0e74b12c84f1b0000d1");
        private static readonly ObjectId DonationId = new("5f43a0e74b12c84f1b0000e1");

        [TestMethod]
        public void Create_ValidInput_LinksContributionToDonation()
        {
            var allocation = DonationAllocation.Create(ContributionId, DonationId, 2);

            allocation.Id.Should().NotBe(ObjectId.Empty);
            allocation.ContributionId.Should().Be(ContributionId);
            allocation.DonationId.Should().Be(DonationId);
            allocation.Hours.Should().Be(2);
            allocation.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1));
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(-1)]
        public void Create_HoursNotPositive_Throws(int hours)
        {
            var act = () => DonationAllocation.Create(ContributionId, DonationId, hours);

            act.Should().Throw<ArgumentOutOfRangeException>();
        }
    }
}