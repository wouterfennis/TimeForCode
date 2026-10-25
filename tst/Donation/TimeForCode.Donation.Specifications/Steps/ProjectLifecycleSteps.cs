using AwesomeAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using Reqnroll;
using TimeForCode.Donation.Api.Client;
using TimeForCode.Donation.Domain;
using ProjectLifecycleResponse = TimeForCode.Donation.Api.Client.ProjectLifecycleResult;
using ProjectStatus = TimeForCode.Donation.Values.ProjectStatus;

namespace TimeForCode.Donation.Specifications.Steps
{
    /// <summary>Moving a project through draft, review, active and archived, and the maintainer's view of it.</summary>
    [Binding]
    internal class ProjectLifecycleSteps
    {
        private readonly IDonationClient _donationClient;
        private readonly DonationContext _context;
        private readonly DonationFixtures _fixtures;

        private ProjectLifecycleResponse? _lifecycleResult;
        private GetMaintainedProjectResult? _maintainedProject;
        private ProjectTransitionResult? _donationCheck;

        public ProjectLifecycleSteps(IDonationClient donationClient, DonationContext context, DonationFixtures fixtures)
        {
            _donationClient = donationClient;
            _context = context;
            _fixtures = fixtures;
        }

        [Given("The user has a project in draft")]
        public void GivenTheUserHasAProjectInDraft()
        {
            _fixtures.SetupLifecycleProject(ProjectStatus.Draft);
        }

        [Given("The user has an active project")]
        public void GivenTheUserHasAnActiveProject()
        {
            _fixtures.SetupLifecycleProject(ProjectStatus.Active);
        }

        [Given("The user has an archived project")]
        public void GivenTheUserHasAnArchivedProject()
        {
            _fixtures.SetupLifecycleProject(ProjectStatus.Archived);
        }

        [Given("There is a project pending approval")]
        public void GivenThereIsAProjectPendingApproval()
        {
            _fixtures.SetupLifecycleProject(ProjectStatus.PendingApproval);
        }

        [Given("^There is a project that is (in draft|pending approval|active|archived)$")]
        public void GivenThereIsAProjectThatIs(string state)
        {
            var status = state switch
            {
                "in draft" => ProjectStatus.Draft,
                "pending approval" => ProjectStatus.PendingApproval,
                "active" => ProjectStatus.Active,
                _ => ProjectStatus.Archived
            };
            _fixtures.SetupLifecycleProject(status);
        }

        [Given("The user has a project in draft with the reviewer reason {string}")]
        public void GivenTheUserHasAProjectInDraftWithTheReviewerReason(string reason)
        {
            _fixtures.SetupLifecycleProject(ProjectStatus.PendingApproval);
            _context.LifecycleProject!.RequestChanges(reason);
        }

        [When("The user submits the project for review")]
        [When("The user submits for review the project")]
        public Task WhenTheUserSubmitsTheProjectForReviewAsync()
        {
            return RunLifecycleActionAsync(id => _donationClient.SubmitProjectForReviewAsync(id));
        }

        [When("The administrator approves the project")]
        [When("The user approves the project")]
        public Task WhenTheAdministratorApprovesTheProjectAsync()
        {
            return RunLifecycleActionAsync(id => _donationClient.ApproveProjectAsync(id));
        }

        [When("The administrator requests changes with the reason {string}")]
        [When("The user requests changes with the reason {string}")]
        public Task WhenTheAdministratorRequestsChangesAsync(string reason)
        {
            return RunLifecycleActionAsync(id => _donationClient.RequestProjectChangesAsync(id, new RequestProjectChangesRequest { Reason = reason }));
        }

        [When("The user archives the project")]
        public Task WhenTheUserArchivesTheProjectAsync()
        {
            return RunLifecycleActionAsync(id => _donationClient.ArchiveProjectAsync(id));
        }

        [When("The user re-activates the project")]
        public Task WhenTheUserReactivatesTheProjectAsync()
        {
            return RunLifecycleActionAsync(id => _donationClient.ReactivateProjectAsync(id));
        }

        [When("The user views their project")]
        public Task WhenTheUserViewsTheirProjectAsync()
        {
            return _context.CaptureAsync(async () => _maintainedProject = await _donationClient.GetMaintainedProjectAsync(_context.LifecycleProject!.Id.ToString()));
        }

        [When("A donor donates to the project")]
        public void WhenADonorDonatesToTheProject()
        {
            // The API path is covered by PledgeDonation.feature; this scenario checks the domain rule directly.
            _donationCheck = _context.LifecycleProject!.EnsureCanReceiveDonations();
        }

        [Then("The user sees the project in draft")]
        public void ThenTheUserSeesTheProjectInDraft()
        {
            _context.LastException.Should().BeNull();
            if (_context.MyProjects != null)
            {
                _context.MyProjects.Projects.Should().Contain(p => p.Project.Status.ToString() == ProjectStatus.Draft.ToString());
                return;
            }

            _maintainedProject.Should().NotBeNull();
            _maintainedProject!.Project.Status.ToString().Should().Be(ProjectStatus.Draft.ToString());
        }

        [Then("The user sees the reviewer reason {string}")]
        public void ThenTheUserSeesTheReviewerReason(string reason)
        {
            _maintainedProject.Should().NotBeNull();
            _maintainedProject!.ReviewerReason.Should().Be(reason);
        }

        [Then("The project is pending approval")]
        public void ThenTheProjectIsPendingApproval()
        {
            AssertLifecycleStatus(ProjectStatus.PendingApproval);
        }

        [Then("The project is active")]
        public void ThenTheProjectIsActive()
        {
            AssertLifecycleStatus(ProjectStatus.Active);
        }

        [Then("The project is back in draft")]
        public void ThenTheProjectIsBackInDraft()
        {
            AssertLifecycleStatus(ProjectStatus.Draft);
        }

        [Then("The project is archived")]
        public void ThenTheProjectIsArchived()
        {
            AssertLifecycleStatus(ProjectStatus.Archived);
        }

        [Then("The project shows the reviewer reason {string}")]
        public void ThenTheProjectShowsTheReviewerReason(string reason)
        {
            _lifecycleResult.Should().NotBeNull();
            _lifecycleResult!.ReviewerReason.Should().Be(reason);
            _context.LifecycleProject!.ChangeRequestReason.Should().Be(reason);
        }

        [Then("The user is informed the project cannot receive donations")]
        public void ThenTheUserIsInformedTheProjectCannotReceiveDonations()
        {
            if (_context.LastException != null)
            {
                _context.LastException.StatusCode.Should().Be(StatusCodes.Status409Conflict);
                return;
            }

            _donationCheck.Should().NotBeNull();
            _donationCheck!.IsSuccess.Should().BeFalse();
        }

        private async Task RunLifecycleActionAsync(Func<string, Task<ProjectLifecycleResponse>> action)
        {
            string projectId = _context.RegisteredProjectId ?? Constants.TestProjectId;

            _fixtures.ProjectRepository.Setup(x => x.UpdateAsync(It.IsAny<Project>()))
                .Returns(Task.CompletedTask);

            await _context.CaptureAsync(async () => _lifecycleResult = await action(projectId));
        }

        private void AssertLifecycleStatus(ProjectStatus expected)
        {
            _context.LastException.Should().BeNull();
            _lifecycleResult.Should().NotBeNull();
            _lifecycleResult!.Status.ToString().Should().Be(expected.ToString());

            _fixtures.ProjectRepository.Verify(x => x.UpdateAsync(It.Is<Project>(p => p.Status == expected)), Times.Once);
        }
    }
}