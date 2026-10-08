using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Reqnroll;
using TimeForCode.Donation.Api.Client;
using TimeForCode.Donation.Application.Interfaces;
using TimeForCode.Donation.Domain;
using TimeForCode.Donation.Specifications.TestBuilder;
using ProjectLifecycleResponse = TimeForCode.Donation.Api.Client.ProjectLifecycleResult;
using ProjectStatus = TimeForCode.Donation.Values.ProjectStatus;

namespace TimeForCode.Donation.Specifications.Steps
{
    internal partial class ProjectSteps
    {
        private ProjectLifecycleResponse? _lifecycleResult;
        private GetMaintainedProjectResult? _maintainedProject;
        private Project? _lifecycleProject;
        private ProjectTransitionResult? _donationCheck;

        [Given("The user has a project in draft")]
        public void GivenTheUserHasAProjectInDraft()
        {
            SetupLifecycleProject(ProjectStatus.Draft);
        }

        [Given("The user has an active project")]
        public void GivenTheUserHasAnActiveProject()
        {
            SetupLifecycleProject(ProjectStatus.Active);
        }

        [Given("The user has an archived project")]
        public void GivenTheUserHasAnArchivedProject()
        {
            SetupLifecycleProject(ProjectStatus.Archived);
        }

        [Given("There is a project pending approval")]
        public void GivenThereIsAProjectPendingApproval()
        {
            SetupLifecycleProject(ProjectStatus.PendingApproval);
        }

        [Given("^There is a project that is (in draft|active|archived)$")]
        public void GivenThereIsAProjectThatIs(string state)
        {
            var status = state switch
            {
                "in draft" => ProjectStatus.Draft,
                "active" => ProjectStatus.Active,
                _ => ProjectStatus.Archived
            };
            SetupLifecycleProject(status);
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

        [Given("The user has a project in draft with the reviewer reason {string}")]
        public void GivenTheUserHasAProjectInDraftWithTheReviewerReason(string reason)
        {
            SetupLifecycleProject(ProjectStatus.PendingApproval);
            _lifecycleProject!.RequestChanges(reason);
        }

        [When("The user views their project")]
        public async Task WhenTheUserViewsTheirProjectAsync()
        {
            try
            {
                _maintainedProject = await _donationClient.GetMaintainedProjectAsync(_lifecycleProject!.Id.ToString());
            }
            catch (ApiException<ProblemDetails> exception)
            {
                _exception = exception;
            }
            catch (ApiException exception)
            {
                _exception = exception;
            }
        }

        [Then("The user sees the project in draft")]
        public void ThenTheUserSeesTheProjectInDraft()
        {
            _exception.Should().BeNull();
            _maintainedProject.Should().NotBeNull();
            _maintainedProject!.Project.Status.ToString().Should().Be(ProjectStatus.Draft.ToString());
        }

        [Then("The user sees the reviewer reason {string}")]
        public void ThenTheUserSeesTheReviewerReason(string reason)
        {
            _maintainedProject.Should().NotBeNull();
            _maintainedProject!.ReviewerReason.Should().Be(reason);
        }

        [When("A donor donates to the project")]
        public void WhenADonorDonatesToTheProject()
        {
            // TODO(review): no donation endpoint exists yet; the rule is exercised directly on the domain.
            _donationCheck = _lifecycleProject!.EnsureCanReceiveDonations();
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
            _lifecycleProject!.ChangeRequestReason.Should().Be(reason);
        }

        [Then("The user is informed the transition is not allowed")]
        public void ThenTheUserIsInformedTheTransitionIsNotAllowed()
        {
            _exception.Should().NotBeNull();
            _exception!.StatusCode.Should().Be(StatusCodes.Status409Conflict);
        }

        [Then("The user is informed the project cannot receive donations")]
        public void ThenTheUserIsInformedTheProjectCannotReceiveDonations()
        {
            _donationCheck.Should().NotBeNull();
            _donationCheck!.IsSuccess.Should().BeFalse();
        }

        private void SetupLifecycleProject(ProjectStatus status)
        {
            _lifecycleProject = ProjectBuilder.Build(status, Constants.TestUserId);

            var mockProjectRepository = _provider.GetRequiredService<Mock<IProjectRepository>>();
            mockProjectRepository.Setup(x => x.GetByIdAsync(_lifecycleProject.Id.ToString()))
                .ReturnsAsync(_lifecycleProject);
            mockProjectRepository.Setup(x => x.UpdateAsync(It.IsAny<Project>()))
                .Returns(Task.CompletedTask);
        }

        private async Task RunLifecycleActionAsync(Func<string, Task<ProjectLifecycleResponse>> action)
        {
            var projectId = _registeredProjectId ?? Constants.TestProjectId;

            var mockProjectRepository = _provider.GetRequiredService<Mock<IProjectRepository>>();
            mockProjectRepository.Setup(x => x.UpdateAsync(It.IsAny<Project>()))
                .Returns(Task.CompletedTask);

            try
            {
                _lifecycleResult = await action(projectId);
            }
            catch (ApiException<ProblemDetails> exception)
            {
                _exception = exception;
            }
            catch (ApiException exception)
            {
                _exception = exception;
            }
        }

        private void AssertLifecycleStatus(ProjectStatus expected)
        {
            _exception.Should().BeNull();
            _lifecycleResult.Should().NotBeNull();
            _lifecycleResult!.Status.ToString().Should().Be(expected.ToString());

            var mockProjectRepository = _provider.GetRequiredService<Mock<IProjectRepository>>();
            mockProjectRepository.Verify(x => x.UpdateAsync(It.Is<Project>(p => p.Status == expected)), Times.Once);
        }
    }
}