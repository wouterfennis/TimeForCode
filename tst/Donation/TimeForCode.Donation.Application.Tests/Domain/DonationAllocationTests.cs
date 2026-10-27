using AwesomeAssertions;
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

        [TestMethod]
        public void Reduce_FewerHoursThanAllocated_KeepsTheRemainder()
        {
            var allocation = DonationAllocation.Create(ContributionId, DonationId, 5);

            var reduced = allocation.Reduce(2);

            reduced.Id.Should().Be(allocation.Id);
            reduced.ContributionId.Should().Be(ContributionId);
            reduced.DonationId.Should().Be(DonationId);
            reduced.Hours.Should().Be(3);
            reduced.CreatedAt.Should().Be(allocation.CreatedAt);
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(-1)]
        [DataRow(5)]
        public void Reduce_NotBetweenZeroAndAllocatedHours_Throws(int hours)
        {
            var allocation = DonationAllocation.Create(ContributionId, DonationId, 5);

            var act = () => allocation.Reduce(hours);

            act.Should().Throw<ArgumentOutOfRangeException>();
        }
    }
}