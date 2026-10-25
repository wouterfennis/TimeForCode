using AwesomeAssertions;
using MongoDB.Bson;
using TimeForCode.Donation.Domain;
using TimeForCode.Donation.Values;

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

        private static Contributor NewContributor() => Contributor.Create("user-1", "octocat", "https://avatars.githubusercontent.com/u/1");

        [TestMethod]
        public void Create_NewContributor_HasAnEmptyProfile()
        {
            var contributor = NewContributor();

            contributor.Role.Should().BeNull();
            contributor.Skills.Should().BeEmpty();
            contributor.HoursAvailable.Should().BeNull();
            contributor.DonorOrganizationId.Should().BeNull();
        }

        [TestMethod]
        public void UpdateProfile_ValidValues_SetsTheProfile()
        {
            var contributor = NewContributor();
            var organizationId = ObjectId.GenerateNewId();

            contributor.UpdateProfile("Backend developer", ["C#", "MongoDB"], 10, organizationId);

            contributor.Role.Should().Be("Backend developer");
            contributor.Skills.Should().Equal("C#", "MongoDB");
            contributor.HoursAvailable.Should().Be(10);
            contributor.DonorOrganizationId.Should().Be(organizationId);
        }

        [TestMethod]
        public void UpdateProfile_NoValues_ClearsTheProfile()
        {
            var contributor = NewContributor();
            contributor.UpdateProfile("Backend developer", ["C#"], 10, ObjectId.GenerateNewId());

            contributor.UpdateProfile(null, null, null, null);

            contributor.Role.Should().BeNull();
            contributor.Skills.Should().BeEmpty();
            contributor.HoursAvailable.Should().BeNull();
            contributor.DonorOrganizationId.Should().BeNull();
        }

        [TestMethod]
        public void UpdateProfile_NullSkill_DropsIt()
        {
            var contributor = Contributor.Create("user-1", "octocat", "https://avatars.githubusercontent.com/u/1");

            contributor.UpdateProfile(null, [null!, "C#"], null, null);

            contributor.Skills.Should().Equal("C#");
        }

        [TestMethod]
        public void UpdateProfile_SkillsWithBlanksAndDuplicates_NormalisesThem()
        {
            var contributor = NewContributor();

            contributor.UpdateProfile(null, [" C# ", "c#", "", "  ", "MongoDB", "C#"], null, null);

            contributor.Skills.Should().Equal("C#", "MongoDB");
        }

        [TestMethod]
        [DataRow("")]
        [DataRow("   ")]
        public void UpdateProfile_BlankRole_ClearsTheRole(string role)
        {
            var contributor = NewContributor();

            contributor.UpdateProfile(role, null, null, null);

            contributor.Role.Should().BeNull();
        }

        [TestMethod]
        public void UpdateProfile_RoleWithSurroundingSpaces_TrimsTheRole()
        {
            var contributor = NewContributor();

            contributor.UpdateProfile("  Designer ", null, null, null);

            contributor.Role.Should().Be("Designer");
        }

        [TestMethod]
        public void UpdateProfile_TooLongRole_Throws()
        {
            var contributor = NewContributor();

            Action act = () => contributor.UpdateProfile(new string('a', ContributorProfileLimits.MaxRoleLength + 1), null, null, null);

            act.Should().Throw<ArgumentException>();
        }

        [TestMethod]
        public void UpdateProfile_TooManySkills_Throws()
        {
            var contributor = NewContributor();
            var skills = Enumerable.Range(1, ContributorProfileLimits.MaxSkills + 1).Select(n => $"skill-{n}");

            Action act = () => contributor.UpdateProfile(null, skills, null, null);

            act.Should().Throw<ArgumentException>();
        }

        [TestMethod]
        public void UpdateProfile_TooLongSkill_Throws()
        {
            var contributor = NewContributor();

            Action act = () => contributor.UpdateProfile(null, [new string('a', ContributorProfileLimits.MaxSkillLength + 1)], null, null);

            act.Should().Throw<ArgumentException>();
        }

        [TestMethod]
        [DataRow(-1)]
        [DataRow(ContributorProfileLimits.MaxHoursAvailable + 1)]
        public void UpdateProfile_HoursOutOfRange_Throws(int hours)
        {
            var contributor = NewContributor();

            Action act = () => contributor.UpdateProfile(null, null, hours, null);

            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [TestMethod]
        public void UpdateProfile_InvalidValues_LeavesTheProfileUnchanged()
        {
            var contributor = NewContributor();
            contributor.UpdateProfile("Backend developer", ["C#"], 10, null);

            Action act = () => contributor.UpdateProfile("Designer", ["Figma"], -1, null);

            act.Should().Throw<ArgumentOutOfRangeException>();
            contributor.Role.Should().Be("Backend developer");
            contributor.Skills.Should().Equal("C#");
        }
    }
}