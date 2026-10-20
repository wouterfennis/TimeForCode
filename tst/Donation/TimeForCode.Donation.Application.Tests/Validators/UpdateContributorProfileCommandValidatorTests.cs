using FluentAssertions;
using TimeForCode.Donation.Application.Validators;
using TimeForCode.Donation.Commands;
using TimeForCode.Donation.Values;

namespace TimeForCode.Donation.Application.Tests.Validators
{
    [TestClass]
    public class UpdateContributorProfileCommandValidatorTests
    {
        private readonly UpdateContributorProfileCommandValidator _sut = new();

        private static UpdateContributorProfileCommand Command(
            string? role = "Backend developer",
            List<string>? skills = null,
            int? hoursAvailable = 10,
            string? donorOrganizationId = null) => new()
            {
                CallerUserId = "user-1",
                GithubLogin = "octocat",
                AvatarUrl = "https://avatars.githubusercontent.com/u/1",
                Role = role,
                Skills = skills ?? ["C#"],
                HoursAvailable = hoursAvailable,
                DonorOrganizationId = donorOrganizationId
            };

        [TestMethod]
        public void Validate_ValidCommand_IsValid()
        {
            _sut.Validate(Command()).IsValid.Should().BeTrue();
        }

        [TestMethod]
        public void Validate_EmptyProfile_IsValid()
        {
            _sut.Validate(Command(role: null, skills: [], hoursAvailable: null)).IsValid.Should().BeTrue();
        }

        [TestMethod]
        public void Validate_TooLongRole_IsInvalid()
        {
            _sut.Validate(Command(role: new string('a', ContributorProfileLimits.MaxRoleLength + 1))).IsValid.Should().BeFalse();
        }

        [TestMethod]
        public void Validate_TooManySkills_IsInvalid()
        {
            var skills = Enumerable.Range(1, ContributorProfileLimits.MaxSkills + 1).Select(n => $"skill-{n}").ToList();

            _sut.Validate(Command(skills: skills)).IsValid.Should().BeFalse();
        }

        [TestMethod]
        public void Validate_TooLongSkill_IsInvalid()
        {
            var skills = new List<string> { new('a', ContributorProfileLimits.MaxSkillLength + 1) };

            _sut.Validate(Command(skills: skills)).IsValid.Should().BeFalse();
        }

        [TestMethod]
        [DataRow(-1)]
        [DataRow(ContributorProfileLimits.MaxHoursAvailable + 1)]
        public void Validate_HoursOutOfRange_IsInvalid(int hours)
        {
            _sut.Validate(Command(hoursAvailable: hours)).IsValid.Should().BeFalse();
        }

        [TestMethod]
        public void Validate_OrganizationIdIsNotAnObjectId_IsInvalid()
        {
            _sut.Validate(Command(donorOrganizationId: "not-an-id")).IsValid.Should().BeFalse();
        }
    }
}