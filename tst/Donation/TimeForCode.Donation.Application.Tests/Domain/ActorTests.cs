using AwesomeAssertions;
using TimeForCode.Donation.Values;

namespace TimeForCode.Donation.Application.Tests.Domain
{
    /// <summary>
    /// Construction rules of the <see cref="Actor"/> value object.
    /// </summary>
    [TestClass]
    public class ActorTests
    {
        [TestMethod]
        public void Constructor_UserIdAndFlag_KeepsBoth()
        {
            var actor = new Actor("user-1", true);

            actor.UserId.Should().Be("user-1");
            actor.IsAdministrator.Should().BeTrue();
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("  ")]
        public void Constructor_BlankUserId_Throws(string? userId)
        {
            Action act = () => new Actor(userId!, false);

            act.Should().Throw<ArgumentException>();
        }

        [TestMethod]
        public void Equality_SameUserAndFlag_IsEqual()
        {
            new Actor("user-1", false).Should().Be(new Actor("user-1", false));
            new Actor("user-1", false).Should().NotBe(new Actor("user-1", true));
        }
    }
}