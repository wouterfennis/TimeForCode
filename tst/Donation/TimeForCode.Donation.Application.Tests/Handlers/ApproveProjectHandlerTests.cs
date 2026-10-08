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
    public class ApproveProjectHandlerTests
    {
        private Mock<IProjectRepository> _mockRepository = default!;
        private ApproveProjectHandler _sut = default!;

        [TestInitialize]
        public void Setup()
        {
            _mockRepository = new Mock<IProjectRepository>();
            _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Project>())).Returns(Task.CompletedTask);
            _sut = new ApproveProjectHandler(_mockRepository.Object, NullLogger<ApproveProjectHandler>.Instance);
        }

        [TestMethod]
        public async Task Handle_PendingApproval_ActivatesAndPersists()
        {
            var project = ProjectTestData.Build(ProjectStatus.PendingApproval, "user-123");
            _mockRepository.Setup(r => r.GetByIdAsync(project.Id.ToString())).ReturnsAsync(project);

            var result = await _sut.Handle(new ApproveProjectCommand { ProjectId = project.Id.ToString() }, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Status.Should().Be(ProjectStatus.Active);
            _mockRepository.Verify(r => r.UpdateAsync(It.Is<Project>(p => p.Status == ProjectStatus.Active)), Times.Once);
        }

        [TestMethod]
        public async Task Handle_ProjectNotFound_ReturnsFailure()
        {
            _mockRepository.Setup(r => r.GetByIdAsync(It.IsAny<string>())).ReturnsAsync((Project?)null);

            var result = await _sut.Handle(new ApproveProjectCommand { ProjectId = "5f43a0e74b12c84f1b000001" }, CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            result.FailureStatusCode.Should().BeNull();
        }

        [TestMethod]
        [DataRow(ProjectStatus.Draft)]
        [DataRow(ProjectStatus.Active)]
        [DataRow(ProjectStatus.Archived)]
        public async Task Handle_InvalidState_ReturnsConflictAndDoesNotPersist(ProjectStatus status)
        {
            var project = ProjectTestData.Build(status, "user-123");
            _mockRepository.Setup(r => r.GetByIdAsync(project.Id.ToString())).ReturnsAsync(project);

            var result = await _sut.Handle(new ApproveProjectCommand { ProjectId = project.Id.ToString() }, CancellationToken.None);

            result.FailureStatusCode.Should().Be(HttpStatusCode.Conflict);
            _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Project>()), Times.Never);
        }
    }
}