using AwesomeAssertions;
using TimeForCode.Reporting.Domain;
using TimeForCode.Reporting.Values;

namespace TimeForCode.Reporting.Application.Tests.Domain
{
    [TestClass]
    public class ProjectDonationReportTests
    {
        private static ProjectDonation Donation(DonationStatus status, int committed, int logged, string? organizationId = null) =>
            new("507f1f77bcf86cd799439031", organizationId, status, committed, logged, new DateTimeOffset(2026, 10, 1, 9, 0, 0, TimeSpan.Zero));

        private static ProjectContribution Contribution(string contributorId, string login, int hours) =>
            new(contributorId, login, hours);

        [TestMethod]
        public void From_NothingYet_ReturnsAnEmptyReport()
        {
            var report = ProjectDonationReport.From([], []);

            report.HoursReceived.Should().Be(0);
            report.HoursLogged.Should().Be(0);
            report.ProgressPercentage.Should().Be(0);
            report.DonorCount.Should().Be(0);
            report.ContributorCount.Should().Be(0);
            report.Donations.Should().BeEmpty();
            report.Contributors.Should().BeEmpty();
        }

        [TestMethod]
        public void From_ActiveAndCompletedDonations_AreReceivedAndLogged()
        {
            var report = ProjectDonationReport.From(
            [
                Donation(DonationStatus.Active, 10, 4),
                Donation(DonationStatus.Completed, 6, 6)
            ], []);

            report.HoursReceived.Should().Be(16);
            report.HoursLogged.Should().Be(10);
        }

        [TestMethod]
        public void From_PendingAndCancelledDonations_AreListedButNotCounted()
        {
            var report = ProjectDonationReport.From(
            [
                Donation(DonationStatus.Active, 10, 0),
                Donation(DonationStatus.Pending, 5, 0),
                Donation(DonationStatus.Cancelled, 8, 2)
            ], []);

            report.HoursReceived.Should().Be(10);
            report.HoursLogged.Should().Be(0);
            report.Donations.Should().HaveCount(3);
            report.DonorCount.Should().Be(1);
        }

        [TestMethod]
        public void From_Donations_CountsThemPerStatus()
        {
            var report = ProjectDonationReport.From(
            [
                Donation(DonationStatus.Active, 10, 0),
                Donation(DonationStatus.Active, 3, 0),
                Donation(DonationStatus.Pending, 5, 0),
                Donation(DonationStatus.Cancelled, 8, 0),
                Donation(DonationStatus.Completed, 2, 2)
            ], []);

            report.PendingDonations.Should().Be(1);
            report.ActiveDonations.Should().Be(2);
            report.CompletedDonations.Should().Be(1);
            report.CancelledDonations.Should().Be(1);
        }

        [TestMethod]
        [DataRow(10, 4, 40)]
        [DataRow(3, 1, 33)]
        [DataRow(10, 10, 100)]
        [DataRow(10, 12, 100)]
        [DataRow(0, 0, 0)]
        public void From_LoggedAgainstReceived_ReturnsTheProgressInWholePercent(int received, int logged, int expected)
        {
            var report = ProjectDonationReport.From([Donation(DonationStatus.Active, received, logged)], []);

            report.ProgressPercentage.Should().Be(expected);
        }

        [TestMethod]
        public void From_SeveralDonationsOfOneOrganization_CountsThatDonorOnce()
        {
            var report = ProjectDonationReport.From(
            [
                Donation(DonationStatus.Active, 10, 0, "507f1f77bcf86cd799439041"),
                Donation(DonationStatus.Completed, 5, 5, "507f1f77bcf86cd799439041"),
                Donation(DonationStatus.Active, 2, 0, "507f1f77bcf86cd799439042")
            ], []);

            report.DonorCount.Should().Be(2);
        }

        [TestMethod]
        public void From_IndividualDonations_CountsEachPledgeAsADonor()
        {
            var report = ProjectDonationReport.From(
            [
                Donation(DonationStatus.Active, 10, 0),
                Donation(DonationStatus.Active, 2, 0)
            ], []);

            report.DonorCount.Should().Be(2);
        }

        [TestMethod]
        public void From_SeveralContributionsOfOneContributor_AddsTheirHoursUp()
        {
            var report = ProjectDonationReport.From([],
            [
                Contribution("c1", "alice", 3),
                Contribution("c2", "bob", 2),
                Contribution("c1", "alice", 1)
            ]);

            report.ContributorCount.Should().Be(2);
            report.Contributors.Should().Equal(
                new ProjectContributor("c1", "alice", 4),
                new ProjectContributor("c2", "bob", 2));
        }
    }
}