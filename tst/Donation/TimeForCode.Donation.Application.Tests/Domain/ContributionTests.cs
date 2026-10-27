using AwesomeAssertions;
using MongoDB.Bson;
using TimeForCode.Donation.Domain;
using TimeForCode.Donation.Values;

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
        private static readonly Contributor Owner = Contributor.Create("user-1", "octocat", "https://avatars.githubusercontent.com/u/1");

        private static Contribution Logged(Uri? reference = null, int hours = 3)
        {
            return Contribution.Create(ProjectId, Owner, hours, reference, LoggedAt);
        }

        private static Contribution Deleted()
        {
            return Logged().MarkDeleted("user-1", false, Owner, ChangedAt).Contribution!;
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(-1)]
        [DataRow(ContributionLimits.MaxHours + 1)]
        public void Create_HoursOutOfRange_Throws(int hours)
        {
            var act = () => Logged(hours: hours);

            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [TestMethod]
        [DataRow("http://github.com/owner/repo/pull/12")]
        [DataRow("https://example.com/owner/repo/pull/12")]
        public void Create_ReferenceNotAnHttpsGithubLink_Throws(string reference)
        {
            var act = () => Logged(new Uri(reference));

            act.Should().Throw<ArgumentException>();
        }

        [TestMethod]
        [DataRow("https://github.com/owner/repo/pull/12", true)]
        [DataRow("https://GitHub.com/owner/repo/commit/abc", true)]
        [DataRow("http://github.com/owner/repo/pull/12", false)]
        [DataRow("https://example.com/owner/repo/pull/12", false)]
        [DataRow("https://github.com:8443/owner/repo/pull/12", false)]
        [DataRow("https://user@github.com/owner/repo/pull/12", false)]
        [DataRow("https://github.com/owner/repo/pull/12?tab=files", false)]
        [DataRow("https://github.com/", false)]
        public void IsValidGithubReference_Link_FollowsTheRules(string reference, bool expected)
        {
            Contribution.IsValidGithubReference(new Uri(reference)).Should().Be(expected);
        }

        [TestMethod]
        public void Correct_NewHoursAndReference_ReplacesCurrentValues()
        {
            var contribution = Logged(OldReference);

            var change = contribution.Correct("user-1", false, Owner, 5, NewReference, ChangedAt);

            change.IsSuccess.Should().BeTrue();
            change.Contribution!.Hours.Should().Be(5);
            change.Contribution.GithubReference.Should().Be(NewReference);
            change.Contribution.Id.Should().Be(contribution.Id);
            change.Contribution.LoggedAt.Should().Be(LoggedAt);
        }

        [TestMethod]
        [DataRow(5, 2)]
        [DataRow(3, 0)]
        [DataRow(1, -2)]
        public void Correct_NewHours_ReportsTheDifferenceInHours(int hours, int expectedDelta)
        {
            var change = Logged().Correct("user-1", false, Owner, hours, null, ChangedAt);

            change.HoursDelta.Should().Be(expectedDelta);
        }

        [TestMethod]
        public void Correct_WithoutReference_RemovesReference()
        {
            var change = Logged(OldReference).Correct("user-1", false, Owner, 3, null, ChangedAt);

            change.Contribution!.GithubReference.Should().BeNull();
        }

        [TestMethod]
        public void Correct_Contribution_KeepsPreviousValuesAsHistory()
        {
            var change = Logged(OldReference).Correct("admin-1", true, null, 5, null, ChangedAt);

            var entry = change.Contribution!.History.Should().ContainSingle().Subject;
            entry.Hours.Should().Be(3);
            entry.GithubReference.Should().Be(OldReference);
            entry.CorrectedByUserId.Should().Be("admin-1");
            entry.CorrectedAt.Should().Be(ChangedAt);
        }

        [TestMethod]
        public void Correct_CorrectedTwice_AppendsHistoryInOrder()
        {
            var first = Logged().Correct("user-1", false, Owner, 5, null, ChangedAt).Contribution!;

            var second = first.Correct("user-1", false, Owner, 7, null, ChangedAt.AddDays(1)).Contribution!;

            second.History.Select(h => h.Hours).Should().Equal(3, 5);
        }

        [TestMethod]
        public void Correct_OtherUser_IsNotAllowed()
        {
            var change = Logged().Correct("user-2", false, Owner, 5, null, ChangedAt);

            change.IsSuccess.Should().BeFalse();
            change.Failure.Should().Be(ContributionChangeFailure.NotAllowed);
            change.Contribution.Should().BeNull();
        }

        [TestMethod]
        public void Correct_ContributorAnonymisedAndCallerNotAdministrator_IsNotAllowed()
        {
            var change = Logged().Correct("user-1", false, null, 5, null, ChangedAt);

            change.Failure.Should().Be(ContributionChangeFailure.NotAllowed);
        }

        [TestMethod]
        public void Correct_Deleted_IsRefusedEvenForAdministrator()
        {
            var change = Deleted().Correct("admin-1", true, null, 5, null, ChangedAt);

            change.Failure.Should().Be(ContributionChangeFailure.Deleted);
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(-2)]
        [DataRow(ContributionLimits.MaxHours + 1)]
        public void Correct_HoursOutOfRange_Throws(int hours)
        {
            var act = () => Logged().Correct("user-1", false, Owner, hours, null, ChangedAt);

            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [TestMethod]
        public void Correct_ReferenceNotAnHttpsGithubLink_Throws()
        {
            var act = () => Logged().Correct("user-1", false, Owner, 3, new Uri("https://example.com/owner/repo"), ChangedAt);

            act.Should().Throw<ArgumentException>();
        }

        [TestMethod]
        public void MarkDeleted_Contributor_RecordsWhoAndWhenAndKeepsValues()
        {
            var contribution = Logged(OldReference);

            var change = contribution.MarkDeleted("user-1", false, Owner, ChangedAt);

            change.IsSuccess.Should().BeTrue();
            change.Contribution!.IsDeleted.Should().BeTrue();
            change.Contribution.DeletedByUserId.Should().Be("user-1");
            change.Contribution.DeletedAt.Should().Be(ChangedAt);
            change.Contribution.Hours.Should().Be(3);
            change.Contribution.GithubReference.Should().Be(OldReference);
        }

        [TestMethod]
        public void MarkDeleted_Contribution_ReportsAllHoursAsRemoved()
        {
            Logged(hours: 3).MarkDeleted("user-1", false, Owner, ChangedAt).HoursDelta.Should().Be(-3);
        }

        [TestMethod]
        public void MarkDeleted_OtherUser_IsNotAllowed()
        {
            Logged().MarkDeleted("user-2", false, Owner, ChangedAt).Failure.Should().Be(ContributionChangeFailure.NotAllowed);
        }

        [TestMethod]
        public void MarkDeleted_Administrator_DeletesContributionOfAnotherUser()
        {
            Logged().MarkDeleted("admin-1", true, null, ChangedAt).IsSuccess.Should().BeTrue();
        }

        [TestMethod]
        public void MarkDeleted_AlreadyDeleted_IsRefused()
        {
            Deleted().MarkDeleted("user-1", false, Owner, ChangedAt).Failure.Should().Be(ContributionChangeFailure.Deleted);
        }

        [TestMethod]
        public void IsDeleted_NewContribution_IsFalse()
        {
            Logged().IsDeleted.Should().BeFalse();
        }

        [TestMethod]
        public void Correct_DeletedWithInvalidHours_IsRefusedInsteadOfThrowing()
        {
            var change = Deleted().Correct("user-1", false, Owner, 0, null, ChangedAt);

            change.Failure.Should().Be(ContributionChangeFailure.Deleted);
        }

        [TestMethod]
        public void Correct_OtherUserWithInvalidHours_IsRefusedInsteadOfThrowing()
        {
            var change = Logged().Correct("user-2", false, Owner, 0, null, ChangedAt);

            change.Failure.Should().Be(ContributionChangeFailure.NotAllowed);
        }

        [TestMethod]
        public void Correct_Contribution_KeepsTheVersionItWasLoadedWith()
        {
            var loaded = new Contribution
            {
                Id = ObjectId.GenerateNewId(),
                ProjectId = ProjectId,
                ContributorId = Owner.Id,
                ContributorGitHubLogin = Owner.GithubLogin,
                ContributorAvatarUrl = Owner.AvatarUrl,
                Hours = 3,
                LoggedAt = LoggedAt,
                Version = 4
            };

            loaded.Correct("user-1", false, Owner, 5, null, ChangedAt).Contribution!.Version.Should().Be(4);
        }
    }
}