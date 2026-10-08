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
    public class GetMaintainedProjectHandlerTests
    {
        private Mock<IProjectRepository> _mockRepository = default!;
        private GetMaintainedProjectHandler _sut = default!;

        [TestInitialize]
        public void Setup()
        {
            _mockRepository = new Mock<IProjectRepository>();
            _sut = new GetMaintainedProjectHandler(_mockRepository.Object, NullLogger<GetMaintainedProjectHandler>.Instance);
        }

        [TestMethod]
        public async Task Handle_OwnerOfDraftProject_ReturnsProjectWithStatus()
        {
            var project = ProjectTestData.Build(ProjectStatus.Draft, "user-123");
            _mockRepository.Setup(r => r.GetByIdAsync(project.Id.ToString())).ReturnsAsync(project);

            var result = await _sut.Handle(new GetMaintainedProjectQuery { ProjectId = project.Id.ToString(), UserId = "user-123" }, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Project.Status.Should().Be(ProjectStatus.Draft);
        }

        [TestMethod]
        public async Task Handle_OwnerOfProjectWithRequestedChanges_ReturnsReviewerReason()
        {
            var project = ProjectTestData.Build(ProjectStatus.PendingApproval, "user-123");
            project.RequestChanges("Please add a description.");
            _mockRepository.Setup(r => r.GetByIdAsync(project.Id.ToString())).ReturnsAsync(project);

            var result = await _sut.Handle(new GetMaintainedProjectQuery { ProjectId = project.Id.ToString(), UserId = "user-123" }, CancellationToken.None);

            result.Value.ReviewerReason.Should().Be("Please add a description.");
        }

        [TestMethod]
        public async Task Handle_ProjectNotFound_ReturnsNotFound()
        {
            _mockRepository.Setup(r => r.GetByIdAsync(It.IsAny<string>())).ReturnsAsync((Project?)null);

            var result = await _sut.Handle(new GetMaintainedProjectQuery { ProjectId = "5f43a0e74b12c84f1b000001", UserId = "user-123" }, CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            result.FailureStatusCode.Should().BeNull();
        }

        [TestMethod]
        public async Task Handle_DifferentOwner_ReturnsForbidden()
        {
            var project = ProjectTestData.Build(ProjectStatus.Draft, "other-user");
            _mockRepository.Setup(r => r.GetByIdAsync(project.Id.ToString())).ReturnsAsync(project);

            var result = await _sut.Handle(new GetMaintainedProjectQuery { ProjectId = project.Id.ToString(), UserId = "user-123" }, CancellationToken.None);

            result.FailureStatusCode.Should().Be(HttpStatusCode.Forbidden);
        }
    }
}