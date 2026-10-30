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

        private static DonationAllocation Allocation(int hours)
        {
            return DonationAllocation.Create(ContributionId, ObjectId.GenerateNewId(), hours);
        }

        [TestMethod]
        public void PlanRelease_SingleAllocationWithEnoughHours_ShrinksIt()
        {
            var allocation = Allocation(5);

            var plan = DonationAllocation.PlanRelease([allocation], 2);

            var release = plan.Should().ContainSingle().Subject;
            release.Allocation.Should().BeSameAs(allocation);
            release.Hours.Should().Be(2);
            release.Remaining!.Hours.Should().Be(3);
        }

        [TestMethod]
        public void PlanRelease_AllHoursOfAllocation_RemovesIt()
        {
            var plan = DonationAllocation.PlanRelease([Allocation(5)], 5);

            plan.Should().ContainSingle().Which.Remaining.Should().BeNull();
        }

        [TestMethod]
        public void PlanRelease_SpilledOverAllocations_TakesFromNewestFirst()
        {
            var oldest = Allocation(3);
            var newest = Allocation(2);

            var plan = DonationAllocation.PlanRelease([oldest, newest], 3).ToList();

            plan.Select(r => (r.Allocation, r.Hours)).Should().Equal((newest, 2), (oldest, 1));
        }

        [TestMethod]
        public void PlanRelease_MoreHoursThanAllocated_ReleasesWhatIsAllocated()
        {
            var plan = DonationAllocation.PlanRelease([Allocation(2)], 10);

            plan.Sum(r => r.Hours).Should().Be(2);
        }

        [TestMethod]
        public void PlanRelease_NoAllocations_PlansNothing()
        {
            DonationAllocation.PlanRelease([], 3).Should().BeEmpty();
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