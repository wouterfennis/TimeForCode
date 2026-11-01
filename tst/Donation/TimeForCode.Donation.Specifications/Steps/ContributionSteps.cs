using AwesomeAssertions;
using MongoDB.Bson;
using Moq;
using Reqnroll;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using TimeForCode.Donation.Api.Client;
using TimeForCode.Donation.Application.Interfaces;
using TimeForCode.Donation.Domain;
using TimeForCode.Donation.Specifications.Mocking;
using TimeForCode.Donation.Specifications.TestBuilder;

namespace TimeForCode.Donation.Specifications.Steps
{
    /// <summary>Logging hours on a project, correcting or deleting a logged contribution and listing the logged contributions.</summary>
    [Binding]
    internal class ContributionSteps
    {
        private readonly IDonationClient _donationClient;
        private readonly BearerTokenHandler _bearerTokenHandler;
        private readonly DonationContext _context;
        private readonly DonationFixtures _fixtures;

        private ContributionResponse? _contributionResult;

        public ContributionSteps(IDonationClient donationClient, BearerTokenHandler bearerTokenHandler, DonationContext context, DonationFixtures fixtures)
        {
            _donationClient = donationClient;
            _bearerTokenHandler = bearerTokenHandler;
            _context = context;
            _fixtures = fixtures;
        }

        [Given("The user is not yet a contributor on the time for code platform")]
        public void GivenTheUserIsNotYetAContributorOnTheTimeForCodePlatform()
        {
            _fixtures.Contributors.Contributors.Should().BeEmpty();
        }

        [Given("^(The user|Another user) has already logged (\\d+) hours on the project((?: with the GitHub reference \"[^\"]*\")?)$")]
        public void GivenSomeoneHasAlreadyLoggedHoursOnTheProject(string who, int hours, string referenceSuffix)
        {
            Contributor contributor = who == "The user" ? _fixtures.TestUserContributor() : _fixtures.OtherUserContributor();
            _fixtures.LogHours(contributor, hours, ReferenceOf(referenceSuffix));
        }

        [Given("^The user has logged (\\d+) hours that count towards the donation$")]
        public async Task GivenTheUserHasLoggedHoursThatCountTowardsTheDonationAsync(int hours)
        {
            // Logged through the platform, unlike "has already logged", so the hours are allocated to the active donation
            _contributionResult = await _donationClient.AddContributionAsync(
                _context.RegisteredProjectId ?? Constants.TestProjectId,
                new AddContributionRequest { Hours = hours });
            _context.ContributionId = _contributionResult.Id;
        }

        [Given("The user has already deleted the contribution")]
        public async Task GivenTheUserHasAlreadyDeletedTheContributionAsync()
        {
            ContributionChange deletion = StoredContribution().MarkDeleted(Constants.TestUserId, false, _fixtures.TestUserContributor(), DateTimeOffset.UtcNow);
            await _fixtures.Contributions.UpdateAsync(deletion.Contribution!, CancellationToken.None);
        }

        [Given("The user has already logged {int} hours on a published project")]
        public void GivenTheUserHasAlreadyLoggedHoursOnAPublishedProject(int hours)
        {
            _fixtures.PublishActiveProject();
            _fixtures.LogHours(_fixtures.TestUserContributor(), hours);
        }

        [Given("^There is no (project|contribution) with the given identifier$")]
        public void GivenThereIsNoProjectOrContributionWithTheGivenIdentifier(string kind)
        {
            if (kind == "project")
            {
                _context.RegisteredProjectId = ProjectBuilder.IdFor(999);
            }
            else
            {
                _context.ContributionId = ObjectId.GenerateNewId().ToString();
            }
        }

        [Given("There is a published project with logged contributions")]
        public void GivenThereIsAPublishedProjectWithLoggedContributions()
        {
            _fixtures.PublishActiveProject();
            _fixtures.LogHours(_fixtures.TestUserContributor(), 3);
            _fixtures.LogHours(_fixtures.TestUserContributor(), 2);
        }

        [Given("There is a published project with contributions logged at different moments")]
        public void GivenThereIsAPublishedProjectWithContributionsLoggedAtDifferentMoments()
        {
            _fixtures.PublishActiveProject();
            var projectId = DonationFixtures.ObjectIdOf(_context.RegisteredProjectId!);
            var contributor = _fixtures.TestUserContributor();

            // Stored latest first, so only the handler's ordering can make the list chronological
            _fixtures.Contributions.CreateAsync(Contribution.Create(projectId, contributor, 5, null, DonationFixtures.FirstLoggedAt.AddDays(10)));
            _fixtures.Contributions.CreateAsync(Contribution.Create(projectId, contributor, 1, null, DonationFixtures.FirstLoggedAt));
            _fixtures.Contributions.CreateAsync(Contribution.Create(projectId, contributor, 2, null, DonationFixtures.FirstLoggedAt.AddDays(5)));
        }

        [Given("There is a published project with a contribution that has a GitHub reference")]
        public void GivenThereIsAPublishedProjectWithAContributionThatHasAGitHubReference()
        {
            _fixtures.PublishActiveProject();
            _fixtures.Contributions.CreateAsync(Contribution.Create(
                DonationFixtures.ObjectIdOf(_context.RegisteredProjectId!),
                _fixtures.TestUserContributor(),
                3,
                new Uri("https://github.com/owner/repo/pull/12"),
                DonationFixtures.FirstLoggedAt));
        }

        [Given("Another project has logged contributions")]
        public void GivenAnotherProjectHasLoggedContributions()
        {
            var otherProject = ProjectBuilder.BuildActive(ProjectBuilder.IdFor(200));
            _fixtures.ProjectRepository
                .Setup(x => x.GetByIdAsync(otherProject.Id.ToString()))
                .ReturnsAsync(otherProject);
            _fixtures.Contributions.CreateAsync(Contribution.Create(otherProject.Id, _fixtures.TestUserContributor(), 7, null, DonationFixtures.FirstLoggedAt));
        }

        [When("^The user logs (-?\\d+) hours on the project((?: with the GitHub reference \"[^\"]*\")?)$")]
        public Task WhenTheUserLogsHoursOnTheProjectAsync(int hours, string referenceSuffix)
        {
            return _context.CaptureAsync(async () => _contributionResult = await _donationClient.AddContributionAsync(
                _context.RegisteredProjectId ?? Constants.TestProjectId,
                new AddContributionRequest { Hours = hours, GithubReference = ReferenceOf(referenceSuffix) }));
        }

        [When("^The user corrects the contribution to (-?\\d+) hours((?: with the GitHub reference \"[^\"]*\"| without a GitHub reference)?)$")]
        public Task WhenTheUserCorrectsTheContributionAsync(int hours, string referenceSuffix)
        {
            // The reference is optional on a correction as well: without one the correction removes it
            return _context.CaptureAsync(async () => _contributionResult = await _donationClient.CorrectContributionAsync(
                _context.RegisteredProjectId ?? Constants.TestProjectId,
                _context.ContributionId!,
                new CorrectContributionRequest { Hours = hours, GithubReference = ReferenceOf(referenceSuffix) }));
        }

        [When("^The user deletes the contribution((?: of \\d+ hours)?)$")]
        public Task WhenTheUserDeletesTheContributionAsync(string hoursSuffix)
        {
            // The suffix is empty when the scenario has a single contribution, otherwise ` of <n> hours`
            if (hoursSuffix.Length > 0)
            {
                int hours = int.Parse(hoursSuffix.Split(' ')[2]);
                _context.ContributionId = _fixtures.Contributions.Contributions.Single(c => c.Hours == hours && !c.IsDeleted).Id.ToString();
            }

            return _context.CaptureAsync(() => _donationClient.DeleteContributionAsync(
                _context.RegisteredProjectId ?? Constants.TestProjectId,
                _context.ContributionId!));
        }

        [When("The user requests the contributions of the project( without an account)")]
        public Task WhenTheUserRequestsTheContributionsOfTheProjectAsync()
        {
            return GetContributionsAsync(_context.RegisteredProjectId ?? Constants.TestProjectId);
        }

        [When("The user requests the contributions of the donor organization")]
        public Task WhenTheUserRequestsTheContributionsOfTheDonorOrganizationAsync()
        {
            return _context.CaptureAsync(async () => _context.ContributionsResult = await _donationClient.GetDonorOrganizationContributionsAsync(_context.DonorOrganizationId!));
        }

        [When("^The user requests the contributions of the project with the identifier \"(.*)\"$")]
        public Task WhenTheUserRequestsTheContributionsOfTheProjectWithTheIdentifierAsync(string identifier)
        {
            return GetContributionsAsync(identifier);
        }

        [Then("The contribution is recorded on the time for code platform")]
        public void ThenTheContributionIsRecordedOnTheTimeForCodePlatform()
        {
            _context.LastException.Should().BeNull();
            _contributionResult.Should().NotBeNull();
            _fixtures.Contributions.Contributions.Should().Contain(c => c.Id.ToString() == _contributionResult!.Id);
        }

        [Then("The contribution shows {int} hours")]
        public void ThenTheContributionShowsHours(int hours)
        {
            _contributionResult.Should().NotBeNull();
            _contributionResult!.Hours.Should().Be(hours);
        }

        [Then("The contributions of the project add up to {int} hours")]
        public void ThenTheContributionsOfTheProjectAddUpToHours(int hours)
        {
            var projectId = DonationFixtures.ObjectIdOf(_context.RegisteredProjectId!);
            var stored = _fixtures.Contributions.Contributions.Where(c => c.ProjectId == projectId && !c.IsDeleted);

            stored.Sum(c => c.Hours).Should().Be(hours);
        }

        [Then("The contributions of the donor organization add up to {int} hours")]
        public void ThenTheContributionsOfTheDonorOrganizationAddUpToHours(int hours)
        {
            _context.LastException.Should().BeNull();
            _context.ContributionsResult.Should().NotBeNull();
            _context.ContributionsResult!.Contributions.Sum(c => c.Hours).Should().Be(hours);
        }

        [Then("No hours are logged on the project")]
        public void ThenNoHoursAreLoggedOnTheProject()
        {
            _fixtures.Contributions.Contributions.Should().BeEmpty();
        }

        [Then("The contribution shows the GitHub login and avatar of the user")]
        public void ThenTheContributionShowsTheGitHubLoginAndAvatarOfTheUser()
        {
            _contributionResult.Should().NotBeNull();
            _contributionResult!.ContributorGitHubLogin.Should().Be(Constants.TestUserLogin);
            _contributionResult.ContributorAvatarUrl.Should().Be(Constants.TestUserAvatarUrl);
        }

        [Then("^The contribution (includes|no longer includes) the GitHub reference \"([^\"]*)\"$")]
        public void ThenTheContributionIncludesTheGitHubReference(string includes, string reference)
        {
            // The response of the last call when it succeeded, otherwise the stored contribution (a rejected correction leaves it unchanged)
            string? current = _contributionResult != null ? _contributionResult.GithubReference : StoredContribution().GithubReference?.ToString();

            if (includes == "includes")
            {
                current.Should().Be(reference);
            }
            else
            {
                current.Should().NotBe(reference);
            }
        }

        [Then("^The history of the contribution keeps (\\d+) hours((?: and the GitHub reference \"[^\"]*\")?)$")]
        public void ThenTheHistoryOfTheContributionKeepsHours(int hours, string referenceSuffix)
        {
            IEnumerable<ContributionCorrection> history = StoredContribution().History.Where(h => h.Hours == hours);

            Uri? reference = ReferenceOf(referenceSuffix);
            if (reference != null)
            {
                history = history.Where(h => h.GithubReference == reference);
            }

            history.Should().ContainSingle();
        }

        [Then("^The (correction|deletion) is recorded with the user and the time$")]
        public void ThenTheChangeIsRecordedWithTheUserAndTheTime(string change)
        {
            Contribution stored = StoredContribution();
            string userId = new JwtSecurityTokenHandler().ReadJwtToken(_bearerTokenHandler.Token).Claims.Single(c => c.Type == ClaimTypes.NameIdentifier).Value;
            (string? changedBy, DateTimeOffset? changedAt) = change == "correction"
                ? (stored.History[^1].CorrectedByUserId, stored.History[^1].CorrectedAt)
                : (stored.DeletedByUserId, stored.DeletedAt);

            changedBy.Should().Be(userId);
            changedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1));
        }

        [Then("^The contribution(?: of \\d+ hours)? is no longer listed for the project$")]
        public async Task ThenTheContributionIsNoLongerListedForTheProjectAsync()
        {
            await GetContributionsAsync(_context.RegisteredProjectId ?? Constants.TestProjectId);

            _context.LastException.Should().BeNull();
            _context.ContributionsResult!.Contributions.Should().NotContain(c => c.Id == _context.ContributionId);
        }

        [Then("The contribution is kept on the time for code platform as deleted")]
        public void ThenTheContributionIsKeptOnTheTimeForCodePlatformAsDeleted()
        {
            StoredContribution().IsDeleted.Should().BeTrue();
        }

        [Then("^The user is registered as (?:a|one) contributor on the time for code platform$")]
        public void ThenTheUserIsRegisteredAsOneContributorOnTheTimeForCodePlatform()
        {
            var contributor = _fixtures.Contributors.Contributors.Should().ContainSingle().Subject;
            contributor.PlatformUserId.Should().Be(Constants.TestUserId);
            contributor.GithubLogin.Should().Be(Constants.TestUserLogin);
            contributor.AvatarUrl.Should().Be(Constants.TestUserAvatarUrl);
        }

        [Then("Both contributions are linked to that contributor")]
        public void ThenBothContributionsAreLinkedToThatContributor()
        {
            var contributor = _fixtures.Contributors.Contributors.Single();

            _fixtures.Contributions.Contributions.Should().HaveCount(2);
            _fixtures.Contributions.Contributions.Should().OnlyContain(c => c.ContributorId == contributor.Id);
        }

        [Then("The contributions of the project are returned")]
        public void ThenTheContributionsOfTheProjectAreReturned()
        {
            _context.LastException.Should().BeNull();
            _context.ContributionsResult.Should().NotBeNull();
            _context.ContributionsResult!.Contributions.Should().NotBeEmpty();
        }

        [Then("{int} contributions are returned")]
        public void ThenContributionsAreReturned(int count)
        {
            _context.ContributionsResult.Should().NotBeNull();
            _context.ContributionsResult!.Contributions.Should().HaveCount(count);
        }

        [Then("Each contribution includes the hours, the contributor and the time it was logged")]
        public void ThenEachContributionIncludesTheHoursTheContributorAndTheTimeItWasLogged()
        {
            _context.ContributionsResult.Should().NotBeNull();
            _context.ContributionsResult!.Contributions.Should().OnlyContain(c =>
                c.Hours > 0
                && !string.IsNullOrEmpty(c.ContributorGitHubLogin)
                && !string.IsNullOrEmpty(c.ContributorAvatarUrl)
                && c.LoggedAt != default);
        }

        [Then("The contributions are ordered from earliest to latest")]
        public void ThenTheContributionsAreOrderedFromEarliestToLatest()
        {
            _context.ContributionsResult.Should().NotBeNull();
            _context.ContributionsResult!.Contributions.Should().HaveCount(3);
            _context.ContributionsResult.Contributions.Select(c => c.LoggedAt).Should().BeInAscendingOrder();
        }

        [Then("The contribution includes a link to the GitHub reference")]
        public void ThenTheContributionIncludesALinkToTheGitHubReference()
        {
            _context.ContributionsResult.Should().NotBeNull();
            _context.ContributionsResult!.Contributions.Single().GithubReference
                .Should().Be("https://github.com/owner/repo/pull/12");
        }

        [Then("Only the contributions of the project are returned")]
        public void ThenOnlyTheContributionsOfTheProjectAreReturned()
        {
            _context.ContributionsResult.Should().NotBeNull();
            _context.ContributionsResult!.Contributions.Should().HaveCount(2);
            _context.ContributionsResult.Contributions.Should().OnlyContain(c => c.ProjectId == _context.RegisteredProjectId);
        }

        [Then("An empty list of contributions is returned")]
        public void ThenAnEmptyListOfContributionsIsReturned()
        {
            _context.LastException.Should().BeNull();
            _context.ContributionsResult.Should().NotBeNull();
            _context.ContributionsResult!.Contributions.Should().BeEmpty();
        }

        /// <summary>The stored contribution the scenario works on, deleted or not.</summary>
        private Contribution StoredContribution()
        {
            return _fixtures.Contributions.Contributions.Single(c => c.Id.ToString() == _context.ContributionId);
        }

        /// <summary>The suffix is empty or `without a GitHub reference` without a reference, otherwise ` with the GitHub reference "&lt;url&gt;"`.</summary>
        private static Uri? ReferenceOf(string referenceSuffix)
        {
            var reference = referenceSuffix.Split('"').ElementAtOrDefault(1);
            return reference == null ? null : new Uri(reference, UriKind.RelativeOrAbsolute);
        }

        private Task GetContributionsAsync(string projectId)
        {
            return _context.CaptureAsync(async () => _context.ContributionsResult = await _donationClient.GetContributionsAsync(projectId));
        }
    }
}