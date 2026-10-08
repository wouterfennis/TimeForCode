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
    public class ReactivateProjectHandlerTests
    {
        private Mock<IProjectRepository> _mockRepository = default!;
        private ReactivateProjectHandler _sut = default!;

        [TestInitialize]
        public void Setup()
        {
            _mockRepository = new Mock<IProjectRepository>();
            _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Project>())).Returns(Task.CompletedTask);
            _sut = new ReactivateProjectHandler(_mockRepository.Object, NullLogger<ReactivateProjectHandler>.Instance);
        }

        [TestMethod]
        public async Task Handle_OwnerAndArchived_ActivatesAndPersists()
        {
            var project = ProjectTestData.Build(ProjectStatus.Archived, "user-123");
            _mockRepository.Setup(r => r.GetByIdAsync(project.Id.ToString())).ReturnsAsync(project);

            var result = await _sut.Handle(new ReactivateProjectCommand { ProjectId = project.Id.ToString(), UserId = "user-123" }, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Status.Should().Be(ProjectStatus.Active);
            _mockRepository.Verify(r => r.UpdateAsync(It.Is<Project>(p => p.Status == ProjectStatus.Active)), Times.Once);
        }

        [TestMethod]
        public async Task Handle_ProjectNotFound_ReturnsFailure()
        {
            _mockRepository.Setup(r => r.GetByIdAsync(It.IsAny<string>())).ReturnsAsync((Project?)null);

            var result = await _sut.Handle(new ReactivateProjectCommand { ProjectId = "5f43a0e74b12c84f1b000001", UserId = "user-123" }, CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            result.FailureStatusCode.Should().BeNull();
        }

        [TestMethod]
        public async Task Handle_DifferentOwner_ReturnsForbiddenAndDoesNotPersist()
        {
            var project = ProjectTestData.Build(ProjectStatus.Archived, "other-user");
            _mockRepository.Setup(r => r.GetByIdAsync(project.Id.ToString())).ReturnsAsync(project);

            var result = await _sut.Handle(new ReactivateProjectCommand { ProjectId = project.Id.ToString(), UserId = "user-123" }, CancellationToken.None);

            result.FailureStatusCode.Should().Be(HttpStatusCode.Forbidden);
            _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Project>()), Times.Never);
        }

        [TestMethod]
        public async Task Handle_NotArchived_ReturnsConflictAndDoesNotPersist()
        {
            var project = ProjectTestData.Build(ProjectStatus.Active, "user-123");
            _mockRepository.Setup(r => r.GetByIdAsync(project.Id.ToString())).ReturnsAsync(project);

            var result = await _sut.Handle(new ReactivateProjectCommand { ProjectId = project.Id.ToString(), UserId = "user-123" }, CancellationToken.None);

            result.FailureStatusCode.Should().Be(HttpStatusCode.Conflict);
            _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Project>()), Times.Never);
        }
    }
}