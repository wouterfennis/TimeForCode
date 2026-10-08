using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Net;
using TimeForCode.Donation.Application.Handlers;
using TimeForCode.Donation.Application.Interfaces;
using TimeForCode.Donation.Commands;
using TimeForCode.Donation.Domain;
using TimeForCode.Donation.Values;

namespace TimeForCode.Donation.Application.Tests.Handlers
{
    [TestClass]
    public class SubmitProjectForReviewHandlerTests
    {
        private Mock<IProjectRepository> _mockRepository = default!;
        private SubmitProjectForReviewHandler _sut = default!;

        [TestInitialize]
        public void Setup()
        {
            _mockRepository = new Mock<IProjectRepository>();
            _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Project>())).Returns(Task.CompletedTask);
            _sut = new SubmitProjectForReviewHandler(_mockRepository.Object, NullLogger<SubmitProjectForReviewHandler>.Instance);
        }

        [TestMethod]
        public async Task Handle_OwnerAndDraft_MovesToPendingApprovalAndPersists()
        {
            var project = ProjectTestData.Build(ProjectStatus.Draft, "user-123");
            _mockRepository.Setup(r => r.GetByIdAsync(project.Id.ToString())).ReturnsAsync(project);

            var result = await _sut.Handle(new SubmitProjectForReviewCommand { ProjectId = project.Id.ToString(), UserId = "user-123" }, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Status.Should().Be(ProjectStatus.PendingApproval);
            _mockRepository.Verify(r => r.UpdateAsync(It.Is<Project>(p => p.Status == ProjectStatus.PendingApproval)), Times.Once);
        }

        [TestMethod]
        public async Task Handle_ProjectNotFound_ReturnsFailure()
        {
            _mockRepository.Setup(r => r.GetByIdAsync(It.IsAny<string>())).ReturnsAsync((Project?)null);

            var result = await _sut.Handle(new SubmitProjectForReviewCommand { ProjectId = "5f43a0e74b12c84f1b000001", UserId = "user-123" }, CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            result.FailureStatusCode.Should().BeNull();
        }

        [TestMethod]
        public async Task Handle_DifferentOwner_ReturnsForbiddenAndDoesNotPersist()
        {
            var project = ProjectTestData.Build(ProjectStatus.Draft, "other-user");
            _mockRepository.Setup(r => r.GetByIdAsync(project.Id.ToString())).ReturnsAsync(project);

            var result = await _sut.Handle(new SubmitProjectForReviewCommand { ProjectId = project.Id.ToString(), UserId = "user-123" }, CancellationToken.None);

            result.FailureStatusCode.Should().Be(HttpStatusCode.Forbidden);
            _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Project>()), Times.Never);
        }

        [TestMethod]
        public async Task Handle_NotDraft_ReturnsConflictAndDoesNotPersist()
        {
            var project = ProjectTestData.Build(ProjectStatus.Active, "user-123");
            _mockRepository.Setup(r => r.GetByIdAsync(project.Id.ToString())).ReturnsAsync(project);

            var result = await _sut.Handle(new SubmitProjectForReviewCommand { ProjectId = project.Id.ToString(), UserId = "user-123" }, CancellationToken.None);

            result.FailureStatusCode.Should().Be(HttpStatusCode.Conflict);
            _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Project>()), Times.Never);
        }
    }
}