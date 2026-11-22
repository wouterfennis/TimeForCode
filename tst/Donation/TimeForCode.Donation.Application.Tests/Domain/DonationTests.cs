using AwesomeAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using TimeForCode.Donation.Application.Tests.Handlers;
using TimeForCode.Donation.Domain;
using TimeForCode.Donation.Values;
using DonationEntity = TimeForCode.Donation.Domain.Donation;

namespace TimeForCode.Donation.Application.Tests.Domain
{
    [TestClass]
    public class DonationTests
    {
        private static readonly ObjectId ProjectId = new("507f1f77bcf86cd799439012");
        private static readonly ObjectId OrganizationId = new("507f1f77bcf86cd799439013");
        private static readonly DateTimeOffset Now = new(2026, 11, 21, 10, 0, 0, TimeSpan.Zero);
        private static readonly DateTimeOffset Later = Now.AddHours(1);

        [TestMethod]
        public void Create_ValidPledge_StartsPendingWithoutLoggedHours()
        {
            var donation = DonationEntity.Create(ProjectId, null, "user-1", 20, Now);

            donation.Status.Should().Be(DonationStatus.Pending);
            donation.HoursCommitted.Should().Be(20);
            donation.HoursLogged.Should().Be(0);
            donation.DonorOrganizationId.Should().BeNull();
            donation.PledgedByUserId.Should().Be("user-1");
        }

        [TestMethod]
        public void Create_WithTime_SetsCreatedAtToThatTime()
        {
            var donation = DonationEntity.Create(ProjectId, null, "user-1", 20, Now);

            donation.CreatedAt.Should().Be(Now);
            donation.UpdatedAt.Should().BeNull();
        }

        [TestMethod]
        public void Activate_WithTime_SetsUpdatedAtToThatTime()
        {
            var donation = Build(DonationStatus.Pending);

            donation.Activate(Later);

            donation.UpdatedAt.Should().Be(Later);
        }

        [TestMethod]
        public void Cancel_WithTime_SetsUpdatedAtToThatTime()
        {
            var donation = Build(DonationStatus.Active);

            donation.Cancel(Later);

            donation.UpdatedAt.Should().Be(Later);
        }

        [TestMethod]
        public void LogHours_WithTime_SetsUpdatedAtToThatTime()
        {
            var donation = Build(DonationStatus.Active);

            donation.LogHours(2, Later);

            donation.UpdatedAt.Should().Be(Later);
        }

        [TestMethod]
        public void ReleaseHours_WithTime_SetsUpdatedAtToThatTime()
        {
            var donation = Build(DonationStatus.Active, logged: 3);

            donation.ReleaseHours(1, Later);

            donation.UpdatedAt.Should().Be(Later);
        }

        [TestMethod]
        public void CountHours_WithTime_SetsUpdatedAtOfCountedDonationsToThatTime()
        {
            var donation = Build(DonationStatus.Active);

            DonationEntity.CountHours([donation], 2, Later).ToList();

            donation.UpdatedAt.Should().Be(Later);
        }

        [TestMethod]
        public void Create_ForOrganization_KeepsOrganizationReference()
        {
            var donation = DonationEntity.Create(ProjectId, OrganizationId, "user-1", 20, Now);

            donation.DonorOrganizationId.Should().Be(OrganizationId);
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(-5)]
        public void Create_NonPositiveHours_Throws(int hours)
        {
            Action act = () => DonationEntity.Create(ProjectId, null, "user-1", hours, Now);

            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [TestMethod]
        public void Activate_Pending_MovesToActive()
        {
            var donation = Build(DonationStatus.Pending);

            var result = donation.Activate(Now);

            result.IsSuccess.Should().BeTrue();
            donation.Status.Should().Be(DonationStatus.Active);
        }

        [TestMethod]
        [DataRow(DonationStatus.Active)]
        [DataRow(DonationStatus.Completed)]
        [DataRow(DonationStatus.Cancelled)]
        public void Activate_NotPending_IsRejected(DonationStatus status)
        {
            var donation = Build(status);

            var result = donation.Activate(Now);

            result.IsSuccess.Should().BeFalse();
            donation.Status.Should().Be(status);
        }

        [TestMethod]
        [DataRow(DonationStatus.Pending)]
        [DataRow(DonationStatus.Active)]
        public void Cancel_PendingOrActive_MovesToCancelled(DonationStatus status)
        {
            var donation = Build(status);

            var result = donation.Cancel(Now);

            result.IsSuccess.Should().BeTrue();
            donation.Status.Should().Be(DonationStatus.Cancelled);
        }

        [TestMethod]
        [DataRow(DonationStatus.Completed)]
        [DataRow(DonationStatus.Cancelled)]
        public void Cancel_Finished_IsRejected(DonationStatus status)
        {
            var donation = Build(status);

            var result = donation.Cancel(Now);

            result.IsSuccess.Should().BeFalse();
            donation.Status.Should().Be(status);
        }

        [TestMethod]
        public void LogHours_ActiveWithRemainingHours_CountsAllHoursAndStaysActive()
        {
            var donation = Build(DonationStatus.Active, committed: 5);

            var applied = donation.LogHours(3, Now);

            applied.Should().Be(3);
            donation.HoursLogged.Should().Be(3);
            donation.Status.Should().Be(DonationStatus.Active);
        }

        [TestMethod]
        public void LogHours_ReachingCommittedHours_Completes()
        {
            var donation = Build(DonationStatus.Active, committed: 5);

            var applied = donation.LogHours(5, Now);

            applied.Should().Be(5);
            donation.Status.Should().Be(DonationStatus.Completed);
        }

        [TestMethod]
        public void LogHours_MoreThanRemaining_CountsOnlyRemainingAndCompletes()
        {
            var donation = Build(DonationStatus.Active, committed: 5, logged: 4);

            var applied = donation.LogHours(10, Now);

            applied.Should().Be(1);
            donation.HoursLogged.Should().Be(5);
            donation.Status.Should().Be(DonationStatus.Completed);
        }

        [TestMethod]
        [DataRow(DonationStatus.Pending)]
        [DataRow(DonationStatus.Completed)]
        [DataRow(DonationStatus.Cancelled)]
        public void LogHours_NotActive_CountsNothing(DonationStatus status)
        {
            var donation = Build(status, committed: 5);
            var loggedBefore = donation.HoursLogged;

            var applied = donation.LogHours(5, Now);

            applied.Should().Be(0);
            donation.HoursLogged.Should().Be(loggedBefore);
            donation.Status.Should().Be(status);
        }

        [TestMethod]
        public void ProgressPercentage_PartlyLogged_IsRoundedDownShare()
        {
            Build(DonationStatus.Active, committed: 10, logged: 4).ProgressPercentage.Should().Be(40);
            Build(DonationStatus.Active, committed: 3, logged: 1).ProgressPercentage.Should().Be(33);
        }

        [TestMethod]
        public void CanBeChangedBy_Pledger_IsAllowed()
        {
            Build(DonationStatus.Pending).CanBeChangedBy(new Actor("user-1", false)).Should().BeTrue();
        }

        [TestMethod]
        public void CanBeChangedBy_Administrator_IsAllowed()
        {
            Build(DonationStatus.Pending).CanBeChangedBy(new Actor("admin", true)).Should().BeTrue();
        }

        [TestMethod]
        public void CanBeChangedBy_OtherUser_IsRejected()
        {
            Build(DonationStatus.Pending).CanBeChangedBy(new Actor("user-2", false)).Should().BeFalse();
        }

        [TestMethod]
        public void CountHours_MoreThanFirstDonationAbsorbs_SpillsOverInGivenOrder()
        {
            var first = Build(DonationStatus.Active, committed: 10, logged: 7);
            var second = Build(DonationStatus.Active, committed: 10);

            var counted = DonationEntity.CountHours([first, second], 8, Now).ToList();

            counted.Select(c => (c.Donation, c.Hours)).Should().Equal((first, 3), (second, 5));
            first.Status.Should().Be(DonationStatus.Completed);
            second.HoursLogged.Should().Be(5);
        }

        [TestMethod]
        public void CountHours_DonationsThatCannotTakeHours_AreSkipped()
        {
            var pending = Build(DonationStatus.Pending);
            var active = Build(DonationStatus.Active);

            var counted = DonationEntity.CountHours([pending, active], 4, Now).ToList();

            counted.Select(c => c.Donation).Should().Equal(active);
        }

        [TestMethod]
        public void CountHours_IsLazy_CountsOnlyWhatIsEnumerated()
        {
            var first = Build(DonationStatus.Active, committed: 10);
            var second = Build(DonationStatus.Active, committed: 10);

            CountedHours firstCounted = DonationEntity.CountHours([first, second], 15, Now).First();

            firstCounted.Donation.Should().BeSameAs(first);
            second.HoursLogged.Should().Be(0);
        }

        [TestMethod]
        [DataRow(nameof(DonationEntity.Status))]
        [DataRow(nameof(DonationEntity.HoursLogged))]
        [DataRow(nameof(DonationEntity.UpdatedAt))]
        [DataRow(nameof(DonationEntity.Version))]
        public void LifecycleProperty_FromOutsideTheAggregate_CannotBeAssigned(string propertyName)
        {
            var setter = typeof(DonationEntity).GetProperty(propertyName)!.SetMethod;

            (setter == null || !setter.IsPublic || setter.ReturnParameter.GetRequiredCustomModifiers().Length > 0)
                .Should().BeTrue($"{propertyName} may only change through the aggregate's own methods");
        }

        [TestMethod]
        public void BsonRoundTrip_DonationWithLoggedHours_KeepsStatusAndHours()
        {
            var donation = Build(DonationStatus.Active, committed: 10, logged: 4);

            var restored = BsonSerializer.Deserialize<DonationEntity>(donation.ToBson());

            restored.Status.Should().Be(DonationStatus.Active);
            restored.HoursLogged.Should().Be(4);
            restored.HoursCommitted.Should().Be(10);
        }

        [TestMethod]
        public void ReleaseHours_ActiveDonation_TakesHoursBackAndStaysActive()
        {
            var donation = Build(DonationStatus.Active, committed: 5, logged: 4);

            var released = donation.ReleaseHours(3, Now);

            released.Should().Be(3);
            donation.HoursLogged.Should().Be(1);
            donation.Status.Should().Be(DonationStatus.Active);
        }

        [TestMethod]
        public void ReleaseHours_CompletedDonation_ReopensAsActive()
        {
            var donation = Build(DonationStatus.Completed, committed: 5, logged: 5);

            donation.ReleaseHours(2, Now);

            donation.HoursLogged.Should().Be(3);
            donation.Status.Should().Be(DonationStatus.Active);
        }

        [TestMethod]
        public void ReleaseHours_CancelledDonation_TakesHoursBackAndStaysCancelled()
        {
            var donation = Build(DonationStatus.Cancelled, committed: 5, logged: 3);

            donation.ReleaseHours(1, Now);

            donation.HoursLogged.Should().Be(2);
            donation.Status.Should().Be(DonationStatus.Cancelled);
        }

        [TestMethod]
        public void ReleaseHours_MoreThanLogged_ReleasesOnlyWhatIsLogged()
        {
            var donation = Build(DonationStatus.Active, committed: 5, logged: 2);

            var released = donation.ReleaseHours(10, Now);

            released.Should().Be(2);
            donation.HoursLogged.Should().Be(0);
        }

        private static DonationEntity Build(DonationStatus status, int committed = 20, int logged = 0)
        {
            return DonationTestData.Build(status, committed: committed, logged: logged);
        }
    }
}