using AwesomeAssertions;
using MongoDB.Bson;
using Reqnroll;
using TimeForCode.Donation.Api.Client;
using TimeForCode.Donation.Domain;

namespace TimeForCode.Donation.Specifications.Steps
{
    /// <summary>The contributor profile of the user and of other users.</summary>
    [Binding]
    internal class ContributorProfileSteps
    {
        private const string ProfileOtherUserId = "another-user-id";

        private readonly IDonationClient _donationClient;
        private readonly DonationContext _context;
        private readonly DonationFixtures _fixtures;

        private ContributorProfileResponse? _profileResult;
        private GetDonorOrganizationContributorsResponse? _organizationContributorsResult;
        private string? _otherContributorId;

        public ContributorProfileSteps(IDonationClient donationClient, DonationContext context, DonationFixtures fixtures)
        {
            _donationClient = donationClient;
            _context = context;
            _fixtures = fixtures;
        }

        private Contributor OtherContributor => _fixtures.Contributors.Contributors.Single(c => c.PlatformUserId == ProfileOtherUserId);

        [Given("The user is registered as a contributor on the time for code platform")]
        public void GivenTheUserIsRegisteredAsAContributorOnTheTimeForCodePlatform()
        {
            _fixtures.TestUserContributor();
        }

        [Given("The user is registered as a contributor with the role {string}")]
        public void GivenTheUserIsRegisteredAsAContributorWithTheRole(string role)
        {
            _fixtures.TestUserContributor().UpdateProfile(role, null, null, null);
        }

        [Given("Another user is registered as a contributor on the time for code platform")]
        public void GivenAnotherUserIsRegisteredAsAContributorOnTheTimeForCodePlatform()
        {
            RegisterOtherContributor();
        }

        [Given("Another user is registered as a contributor affiliated with the donor organization")]
        public void GivenAnotherUserIsRegisteredAsAContributorAffiliatedWithTheDonorOrganization()
        {
            RegisterOtherContributor().UpdateProfile(null, null, null, DonationFixtures.ObjectIdOf(_context.DonorOrganizationId!));
        }

        [Given("There is no contributor with the given identifier")]
        public void GivenThereIsNoContributorWithTheGivenIdentifier()
        {
            _otherContributorId = ObjectId.GenerateNewId().ToString();
        }

        [When("The user requests their own contributor profile")]
        public Task WhenTheUserRequestsTheirOwnContributorProfileAsync()
        {
            return _context.CaptureAsync(async () => _profileResult = await _donationClient.GetMyContributorProfileAsync());
        }

        [When("The user requests the contributor profile of the other user")]
        public Task WhenTheUserRequestsTheContributorProfileOfTheOtherUserAsync()
        {
            return _context.CaptureAsync(async () => _profileResult = await _donationClient.GetContributorProfileAsync(_otherContributorId!));
        }

        [When("The user requests the contributors of the donor organization")]
        public Task WhenTheUserRequestsTheContributorsOfTheDonorOrganizationAsync()
        {
            return _context.CaptureAsync(async () => _organizationContributorsResult =
                await _donationClient.GetDonorOrganizationContributorsAsync(_context.DonorOrganizationId!));
        }

        [When("The user updates their contributor profile with the role {string}")]
        public Task WhenTheUserUpdatesTheirContributorProfileWithTheRoleAsync(string role)
        {
            return UpdateOwnProfileAsync(new UpdateContributorProfileRequest { Role = role });
        }

        [When("The user updates their contributor profile with the role {string}, the skills {string} and {int} available hours")]
        public Task WhenTheUserUpdatesTheirContributorProfileWithTheRoleTheSkillsAndAvailableHoursAsync(string role, string skills, int hours)
        {
            return UpdateOwnProfileAsync(new UpdateContributorProfileRequest { Role = role, Skills = SplitSkills(skills), HoursAvailable = hours });
        }

        [When("The user updates their contributor profile with the skills {string}")]
        public Task WhenTheUserUpdatesTheirContributorProfileWithTheSkillsAsync(string skills)
        {
            return UpdateOwnProfileAsync(new UpdateContributorProfileRequest { Skills = SplitSkills(skills) });
        }

        [When("The user updates their contributor profile with {int} skills")]
        public Task WhenTheUserUpdatesTheirContributorProfileWithSkillsAsync(int count)
        {
            var skills = Enumerable.Range(1, count).Select(n => $"skill-{n}").ToList();

            return UpdateOwnProfileAsync(new UpdateContributorProfileRequest { Skills = skills });
        }

        [When("The user updates their contributor profile with a skill of {int} characters")]
        public Task WhenTheUserUpdatesTheirContributorProfileWithASkillOfCharactersAsync(int length)
        {
            return UpdateOwnProfileAsync(new UpdateContributorProfileRequest { Skills = [new string('a', length)] });
        }

        [When("The user updates their contributor profile with {int} available hours")]
        public Task WhenTheUserUpdatesTheirContributorProfileWithAvailableHoursAsync(int hours)
        {
            return UpdateOwnProfileAsync(new UpdateContributorProfileRequest { HoursAvailable = hours });
        }

        [When("The user links their contributor profile to the donor organization")]
        public Task WhenTheUserLinksTheirContributorProfileToTheDonorOrganizationAsync()
        {
            return UpdateOwnProfileAsync(new UpdateContributorProfileRequest { DonorOrganizationId = _context.DonorOrganizationId });
        }

        [When("The user updates the contributor profile of the other user with the role {string}")]
        public Task WhenTheUserUpdatesTheContributorProfileOfTheOtherUserWithTheRoleAsync(string role)
        {
            return _context.CaptureAsync(async () => _profileResult = await _donationClient.UpdateContributorProfileAsync(
                _otherContributorId!,
                new UpdateContributorProfileRequest { Role = role }));
        }

        [Then("The contributor profile is updated on the time for code platform")]
        public void ThenTheContributorProfileIsUpdatedOnTheTimeForCodePlatform()
        {
            _context.LastException.Should().BeNull();
            _profileResult.Should().NotBeNull();

            var stored = _fixtures.Contributors.Contributors.Single(c => c.PlatformUserId == Constants.TestUserId);
            stored.Id.ToString().Should().Be(_profileResult!.Id);
            stored.Role.Should().Be(_profileResult.Role);
            stored.Skills.Should().Equal(_profileResult.Skills);
            stored.HoursAvailable.Should().Be(_profileResult.HoursAvailable);
            stored.DonorOrganizationId?.ToString().Should().Be(_profileResult.DonorOrganizationId);
        }

        [Then("The contributor profile shows the role {string}")]
        public void ThenTheContributorProfileShowsTheRole(string role)
        {
            _profileResult.Should().NotBeNull();
            _profileResult!.Role.Should().Be(role);
        }

        [Then("The contributor profile shows the skills {string}")]
        public void ThenTheContributorProfileShowsTheSkills(string skills)
        {
            _profileResult.Should().NotBeNull();
            _profileResult!.Skills.Should().Equal(skills.Split(", "));
        }

        [Then("The contributor profile shows {int} available hours")]
        public void ThenTheContributorProfileShowsAvailableHours(int hours)
        {
            _profileResult.Should().NotBeNull();
            _profileResult!.HoursAvailable.Should().Be(hours);
        }

        [Then("The contributor profile shows the affiliation with the donor organization")]
        public void ThenTheContributorProfileShowsTheAffiliationWithTheDonorOrganization()
        {
            _profileResult.Should().NotBeNull();
            _profileResult!.DonorOrganizationId.Should().Be(_context.DonorOrganizationId);
        }

        [Then("The contributor profile is unchanged")]
        public void ThenTheContributorProfileIsUnchanged()
        {
            AssertEmptyProfile(_fixtures.Contributors.Contributors.Single(c => c.PlatformUserId == Constants.TestUserId));
        }

        [Then("The contributor profile of the other user is returned")]
        public void ThenTheContributorProfileOfTheOtherUserIsReturned()
        {
            _context.LastException.Should().BeNull();
            _profileResult.Should().NotBeNull();
            _profileResult!.Id.Should().Be(_otherContributorId);
        }

        [Then("The contributor profile of the other user shows the role {string}")]
        public void ThenTheContributorProfileOfTheOtherUserShowsTheRole(string role)
        {
            _context.LastException.Should().BeNull();
            _profileResult.Should().NotBeNull();
            _profileResult!.Role.Should().Be(role);
            OtherContributor.Role.Should().Be(role);
        }

        [Then("The contributor profile of the other user is unchanged")]
        public void ThenTheContributorProfileOfTheOtherUserIsUnchanged()
        {
            AssertEmptyProfile(OtherContributor);
        }

        [Then("The contributors of the donor organization are returned")]
        public void ThenTheContributorsOfTheDonorOrganizationAreReturned()
        {
            _context.LastException.Should().BeNull();
            _organizationContributorsResult.Should().NotBeNull();
            _organizationContributorsResult!.Contributors.Should().ContainSingle()
                .Which.Id.Should().Be(_otherContributorId);
        }

        private Task UpdateOwnProfileAsync(UpdateContributorProfileRequest request)
        {
            return _context.CaptureAsync(async () => _profileResult = await _donationClient.UpdateMyContributorProfileAsync(request));
        }

        private Contributor RegisterOtherContributor()
        {
            var contributor = Contributor.Create(ProfileOtherUserId, "other-user", "https://avatars.githubusercontent.com/u/2?v=4");
            _fixtures.Contributors.Add(contributor);
            _otherContributorId = contributor.Id.ToString();
            return contributor;
        }

        private static List<string> SplitSkills(string skills)
        {
            return [.. skills.Split(',')];
        }

        private static void AssertEmptyProfile(Contributor contributor)
        {
            contributor.Role.Should().BeNull();
            contributor.Skills.Should().BeEmpty();
            contributor.HoursAvailable.Should().BeNull();
            contributor.DonorOrganizationId.Should().BeNull();
        }
    }
}