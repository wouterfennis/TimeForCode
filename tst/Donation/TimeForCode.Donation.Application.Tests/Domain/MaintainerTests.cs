using AwesomeAssertions;
using TimeForCode.Donation.Domain;

namespace TimeForCode.Donation.Application.Tests.Domain
{
    [TestClass]
    public class MaintainerTests
    {
        [TestMethod]
        public void Create_ValidValues_SetsFields()
        {
            var maintainer = Maintainer.Create("user-1", "octocat", "https://avatars.githubusercontent.com/u/1");

            maintainer.Id.Should().NotBeNull();
            maintainer.PlatformUserId.Should().Be("user-1");
            maintainer.GithubLogin.Should().Be("octocat");
            maintainer.AvatarUrl.Should().Be("https://avatars.githubusercontent.com/u/1");
        }

        [TestMethod]
        [DataRow(null, "octocat", "https://avatars.githubusercontent.com/u/1")]
        [DataRow(" ", "octocat", "https://avatars.githubusercontent.com/u/1")]
        [DataRow("user-1", "", "https://avatars.githubusercontent.com/u/1")]
        [DataRow("user-1", "octocat", null)]
        [DataRow("user-1", "octocat", " ")]
        public void Create_BlankValue_Throws(string? platformUserId, string? githubLogin, string? avatarUrl)
        {
            Action act = () => Maintainer.Create(platformUserId!, githubLogin!, avatarUrl!);

            act.Should().Throw<ArgumentException>();
        }
    }
}