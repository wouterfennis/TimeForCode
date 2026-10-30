using AwesomeAssertions;
using TimeForCode.Reporting.Domain;
using TimeForCode.Reporting.Values;

namespace TimeForCode.Reporting.Application.Tests.Domain
{
    [TestClass]
    public class ImpactReportTests
    {
        private const string ProjectA = "507f1f77bcf86cd799439021";
        private const string ProjectB = "507f1f77bcf86cd799439022";

        private static OrganizationDonation Donation(string projectId, DonationStatus status, int committed, int logged) =>
            new(projectId, status, committed, logged);

        [TestMethod]
        public void From_NoDonations_ReturnsAnEmptyReport()
        {
            var report = ImpactReport.From([]);

            report.HoursPledged.Should().Be(0);
            report.HoursLogged.Should().Be(0);
            report.ProjectsSupported.Should().Be(0);
            report.Projects.Should().BeEmpty();
        }

        [TestMethod]
        public void From_ActiveAndCompletedDonations_AddsUpPledgedAndLoggedHours()
        {
            var report = ImpactReport.From(
            [
                Donation(ProjectA, DonationStatus.Active, 10, 4),
                Donation(ProjectB, DonationStatus.Completed, 6, 6)
            ]);

            report.HoursPledged.Should().Be(16);
            report.HoursLogged.Should().Be(10);
        }

        [TestMethod]
        public void From_PendingAndCancelledDonations_AreNotCounted()
        {
            var report = ImpactReport.From(
            [
                Donation(ProjectA, DonationStatus.Pending, 10, 0),
                Donation(ProjectB, DonationStatus.Cancelled, 6, 2)
            ]);

            report.HoursPledged.Should().Be(0);
            report.HoursLogged.Should().Be(0);
            report.ProjectsSupported.Should().Be(0);
        }

        [TestMethod]
        public void From_SeveralDonationsToOneProject_CountsThatProjectOnce()
        {
            var report = ImpactReport.From(
            [
                Donation(ProjectA, DonationStatus.Active, 10, 4),
                Donation(ProjectA, DonationStatus.Completed, 5, 5)
            ]);

            report.ProjectsSupported.Should().Be(1);
            report.Projects.Should().ContainSingle().Which.Should().Be(new ProjectImpact(ProjectA, 15, 9));
        }

        [TestMethod]
        public void From_DonationsToDifferentProjects_ListsEachProjectInOrderOfTheFirstDonation()
        {
            var report = ImpactReport.From(
            [
                Donation(ProjectB, DonationStatus.Active, 20, 10),
                Donation(ProjectA, DonationStatus.Active, 10, 5)
            ]);

            report.Projects.Should().Equal(new ProjectImpact(ProjectB, 20, 10), new ProjectImpact(ProjectA, 10, 5));
        }
    }
}