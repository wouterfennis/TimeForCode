using FluentAssertions;
using TimeForCode.Donation.Domain;

namespace TimeForCode.Donation.Application.Tests.Domain
{
    [TestClass]
    public class ContributorTests
    {
        [TestMethod]
        public void Create_ValidValues_SetsFields()
        {
            var contributor = Contributor.Create("user-1", "octocat", "https://avatars.githubusercontent.com/u/1");

            contributor.GithubLogin.Should().Be("octocat");
            contributor.AvatarUrl.Should().Be("https://avatars.githubusercontent.com/u/1");
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow(" ")]
        public void Create_BlankAvatarUrl_Throws(string? avatarUrl)
        {
            Action act = () => Contributor.Create("user-1", "octocat", avatarUrl!);

            act.Should().Throw<ArgumentException>();
        }
    }
}