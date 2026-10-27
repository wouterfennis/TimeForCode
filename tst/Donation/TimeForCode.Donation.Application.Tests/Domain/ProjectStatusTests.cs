using AwesomeAssertions;
using TimeForCode.Donation.Values;

namespace TimeForCode.Donation.Application.Tests.Domain
{
    [TestClass]
    public class ProjectStatusTests
    {
        [TestMethod]
        [DataRow(ProjectStatus.Active, 0)]
        [DataRow(ProjectStatus.Archived, 1)]
        [DataRow(ProjectStatus.Draft, 2)]
        [DataRow(ProjectStatus.PendingApproval, 3)]
        public void Value_PersistedStatus_KeepsItsNumber(ProjectStatus status, int persisted)
        {
            ((int)status).Should().Be(persisted);
        }
    }
}