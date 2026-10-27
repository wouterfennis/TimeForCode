using AwesomeAssertions;
using MongoDB.Bson;
using TimeForCode.Donation.Domain;

namespace TimeForCode.Donation.Application.Tests.Domain
{
    [TestClass]
    public class ContributionTests
    {
        private static readonly ObjectId ProjectId = new("5f43a0e74b12c84f1b000001");
        private static readonly DateTimeOffset LoggedAt = new(2024, 3, 1, 9, 0, 0, TimeSpan.Zero);
        private static readonly DateTimeOffset ChangedAt = new(2024, 3, 5, 12, 0, 0, TimeSpan.Zero);
        private static readonly Uri OldReference = new("https://github.com/owner/repo/pull/12");
        private static readonly Uri NewReference = new("https://github.com/owner/repo/pull/34");

        private static Contribution Logged(Uri? reference = null)
        {
            var contributor = Contributor.Create("user-1", "octocat", "https://avatars.githubusercontent.com/u/1");
            return Contribution.Create(ProjectId, contributor, 3, reference, LoggedAt);
        }

        [TestMethod]
        public void Correct_NewHoursAndReference_ReplacesCurrentValues()
        {
            var contribution = Logged(OldReference);

            var corrected = contribution.Correct("user-1", 5, NewReference, ChangedAt);

            corrected.Hours.Should().Be(5);
            corrected.GithubReference.Should().Be(NewReference);
            corrected.Id.Should().Be(contribution.Id);
            corrected.LoggedAt.Should().Be(LoggedAt);
        }

        [TestMethod]
        public void Correct_WithoutReference_RemovesReference()
        {
            var corrected = Logged(OldReference).Correct("user-1", 3, null, ChangedAt);

            corrected.GithubReference.Should().BeNull();
        }

        [TestMethod]
        public void Correct_Contribution_KeepsPreviousValuesAsHistory()
        {
            var corrected = Logged(OldReference).Correct("admin-1", 5, null, ChangedAt);

            var entry = corrected.History.Should().ContainSingle().Subject;
            entry.Hours.Should().Be(3);
            entry.GithubReference.Should().Be(OldReference);
            entry.CorrectedByUserId.Should().Be("admin-1");
            entry.CorrectedAt.Should().Be(ChangedAt);
        }

        [TestMethod]
        public void Correct_CorrectedTwice_AppendsHistoryInOrder()
        {
            var corrected = Logged().Correct("user-1", 5, null, ChangedAt).Correct("user-1", 7, null, ChangedAt.AddDays(1));

            corrected.History.Select(h => h.Hours).Should().Equal(3, 5);
        }

        [TestMethod]
        public void MarkDeleted_Contribution_RecordsWhoAndWhenAndKeepsValues()
        {
            var contribution = Logged(OldReference);

            var deleted = contribution.MarkDeleted("user-1", ChangedAt);

            deleted.IsDeleted.Should().BeTrue();
            deleted.DeletedByUserId.Should().Be("user-1");
            deleted.DeletedAt.Should().Be(ChangedAt);
            deleted.Hours.Should().Be(3);
            deleted.GithubReference.Should().Be(OldReference);
        }

        [TestMethod]
        public void IsDeleted_NewContribution_IsFalse()
        {
            Logged().IsDeleted.Should().BeFalse();
        }

        [TestMethod]
        [DataRow("user-1", "user-1", false, true)]
        [DataRow("user-2", "user-1", false, false)]
        [DataRow("user-2", "user-1", true, true)]
        public void CanBeChangedBy_UserAndRole_FollowsOwnershipOrAdministrator(string userId, string contributorUserId, bool isAdministrator, bool expected)
        {
            var contributor = Contributor.Create(contributorUserId, "octocat", "https://avatars.githubusercontent.com/u/1");
            var contribution = Contribution.Create(ProjectId, contributor, 3, null, LoggedAt);

            contribution.CanBeChangedBy(userId, contributor, isAdministrator).Should().Be(expected);
        }
    }
}