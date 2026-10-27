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

        [TestMethod]
        public void Create_ValidPledge_StartsPendingWithoutLoggedHours()
        {
            var donation = DonationEntity.Create(ProjectId, null, "user-1", 20);

            donation.Status.Should().Be(DonationStatus.Pending);
            donation.HoursCommitted.Should().Be(20);
            donation.HoursLogged.Should().Be(0);
            donation.DonorOrganizationId.Should().BeNull();
            donation.PledgedByUserId.Should().Be("user-1");
        }

        [TestMethod]
        public void Create_ForOrganization_KeepsOrganizationReference()
        {
            var donation = DonationEntity.Create(ProjectId, OrganizationId, "user-1", 20);

            donation.DonorOrganizationId.Should().Be(OrganizationId);
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(-5)]
        public void Create_NonPositiveHours_Throws(int hours)
        {
            Action act = () => DonationEntity.Create(ProjectId, null, "user-1", hours);

            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [TestMethod]
        public void Activate_Pending_MovesToActive()
        {
            var donation = Build(DonationStatus.Pending);

            var result = donation.Activate();

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

            var result = donation.Activate();

            result.IsSuccess.Should().BeFalse();
            donation.Status.Should().Be(status);
        }

        [TestMethod]
        [DataRow(DonationStatus.Pending)]
        [DataRow(DonationStatus.Active)]
        public void Cancel_PendingOrActive_MovesToCancelled(DonationStatus status)
        {
            var donation = Build(status);

            var result = donation.Cancel();

            result.IsSuccess.Should().BeTrue();
            donation.Status.Should().Be(DonationStatus.Cancelled);
        }

        [TestMethod]
        [DataRow(DonationStatus.Completed)]
        [DataRow(DonationStatus.Cancelled)]
        public void Cancel_Finished_IsRejected(DonationStatus status)
        {
            var donation = Build(status);

            var result = donation.Cancel();

            result.IsSuccess.Should().BeFalse();
            donation.Status.Should().Be(status);
        }

        [TestMethod]
        public void LogHours_ActiveWithRemainingHours_CountsAllHoursAndStaysActive()
        {
            var donation = Build(DonationStatus.Active, committed: 5);

            var applied = donation.LogHours(3);

            applied.Should().Be(3);
            donation.HoursLogged.Should().Be(3);
            donation.Status.Should().Be(DonationStatus.Active);
        }

        [TestMethod]
        public void LogHours_ReachingCommittedHours_Completes()
        {
            var donation = Build(DonationStatus.Active, committed: 5);

            var applied = donation.LogHours(5);

            applied.Should().Be(5);
            donation.Status.Should().Be(DonationStatus.Completed);
        }

        [TestMethod]
        public void LogHours_MoreThanRemaining_CountsOnlyRemainingAndCompletes()
        {
            var donation = Build(DonationStatus.Active, committed: 5, logged: 4);

            var applied = donation.LogHours(10);

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

            var applied = donation.LogHours(5);

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
            Build(DonationStatus.Pending).CanBeChangedBy("user-1", isAdministrator: false).Should().BeTrue();
        }

        [TestMethod]
        public void CanBeChangedBy_Administrator_IsAllowed()
        {
            Build(DonationStatus.Pending).CanBeChangedBy("admin", isAdministrator: true).Should().BeTrue();
        }

        [TestMethod]
        public void CanBeChangedBy_OtherUser_IsRejected()
        {
            Build(DonationStatus.Pending).CanBeChangedBy("user-2", isAdministrator: false).Should().BeFalse();
        }

        [TestMethod]
        public void CountHours_MoreThanFirstDonationAbsorbs_SpillsOverInGivenOrder()
        {
            var first = Build(DonationStatus.Active, committed: 10, logged: 7);
            var second = Build(DonationStatus.Active, committed: 10);

            var counted = DonationEntity.CountHours([first, second], 8).ToList();

            counted.Select(c => (c.Donation, c.Hours)).Should().Equal((first, 3), (second, 5));
            first.Status.Should().Be(DonationStatus.Completed);
            second.HoursLogged.Should().Be(5);
        }

        [TestMethod]
        public void CountHours_DonationsThatCannotTakeHours_AreSkipped()
        {
            var pending = Build(DonationStatus.Pending);
            var active = Build(DonationStatus.Active);

            var counted = DonationEntity.CountHours([pending, active], 4).ToList();

            counted.Select(c => c.Donation).Should().Equal(active);
        }

        [TestMethod]
        public void CountHours_IsLazy_CountsOnlyWhatIsEnumerated()
        {
            var first = Build(DonationStatus.Active, committed: 10);
            var second = Build(DonationStatus.Active, committed: 10);

            DonationEntity.CountHours([first, second], 15).First();

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

            var released = donation.ReleaseHours(3);

            released.Should().Be(3);
            donation.HoursLogged.Should().Be(1);
            donation.Status.Should().Be(DonationStatus.Active);
        }

        [TestMethod]
        public void ReleaseHours_CompletedDonation_ReopensAsActive()
        {
            var donation = Build(DonationStatus.Completed, committed: 5, logged: 5);

            donation.ReleaseHours(2);

            donation.HoursLogged.Should().Be(3);
            donation.Status.Should().Be(DonationStatus.Active);
        }

        [TestMethod]
        public void ReleaseHours_CancelledDonation_TakesHoursBackAndStaysCancelled()
        {
            var donation = Build(DonationStatus.Cancelled, committed: 5, logged: 3);

            donation.ReleaseHours(1);

            donation.HoursLogged.Should().Be(2);
            donation.Status.Should().Be(DonationStatus.Cancelled);
        }

        [TestMethod]
        public void ReleaseHours_MoreThanLogged_ReleasesOnlyWhatIsLogged()
        {
            var donation = Build(DonationStatus.Active, committed: 5, logged: 2);

            var released = donation.ReleaseHours(10);

            released.Should().Be(2);
            donation.HoursLogged.Should().Be(0);
        }

        private static DonationEntity Build(DonationStatus status, int committed = 20, int logged = 0)
        {
            return DonationTestData.Build(status, committed: committed, logged: logged);
        }
    }
}