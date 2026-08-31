using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TimeForCode.Donation.Application.Handlers;
using TimeForCode.Donation.Application.Interfaces;
using TimeForCode.Donation.Commands;
using TimeForCode.Donation.Domain;

namespace TimeForCode.Donation.Application.Tests.Handlers
{
    [TestClass]
    public class UpdateDonorOrganizationHandlerTests
    {
        private Mock<IDonorOrganizationRepository> _mockRepository = default!;
        private UpdateDonorOrganizationHandler _sut = default!;

        [TestInitialize]
        public void Setup()
        {
            _mockRepository = new Mock<IDonorOrganizationRepository>();
            _sut = new UpdateDonorOrganizationHandler(_mockRepository.Object, NullLogger<UpdateDonorOrganizationHandler>.Instance);
        }

        [TestMethod]
        public async Task Handle_ValidUpdate_ReturnsSuccess()
        {
            // Arrange
            var org = DonorOrganization.Create("Original Name", "old@org.com", null);
            _mockRepository.Setup(r => r.GetByIdAsync(org.Id.ToString()))
                .ReturnsAsync(org);
            _mockRepository.Setup(r => r.GetByNameAsync("Updated Name"))
                .ReturnsAsync((DonorOrganization?)null);

            var command = new UpdateDonorOrganizationCommand
            {
                Id = org.Id.ToString(),
                Name = "Updated Name",
                ContactEmail = "new@org.com",
                Website = "https://updated.org"
            };

            // Act
            var result = await _sut.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Organization.Name.Should().Be("Updated Name");
            result.Value.Organization.ContactEmail.Should().Be("new@org.com");
            result.Value.Organization.Website.Should().Be("https://updated.org");
            _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<DonorOrganization>()), Times.Once);
        }

        [TestMethod]
        public async Task Handle_NonExistingId_ReturnsFailure()
        {
            // Arrange
            _mockRepository.Setup(r => r.GetByIdAsync("nonexistent"))
                .ReturnsAsync((DonorOrganization?)null);

            var command = new UpdateDonorOrganizationCommand
            {
                Id = "nonexistent",
                Name = "Some Name"
            };

            // Act
            var result = await _sut.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.ErrorMessage.Should().Contain("nonexistent");
        }

        [TestMethod]
        public async Task Handle_DuplicateName_ReturnsConflict()
        {
            // Arrange
            var org = DonorOrganization.Create("Original Name", null, null);
            var otherOrg = DonorOrganization.Create("Taken Name", null, null);
            _mockRepository.Setup(r => r.GetByIdAsync(org.Id.ToString()))
                .ReturnsAsync(org);
            _mockRepository.Setup(r => r.GetByNameAsync("Taken Name"))
                .ReturnsAsync(otherOrg);

            var command = new UpdateDonorOrganizationCommand
            {
                Id = org.Id.ToString(),
                Name = "Taken Name"
            };

            // Act
            var result = await _sut.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.FailureStatusCode.Should().Be(System.Net.HttpStatusCode.Conflict);
            _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<DonorOrganization>()), Times.Never);
        }

        [TestMethod]
        public async Task Handle_SameName_SkipsUniquenessCheck()
        {
            // Arrange
            var org = DonorOrganization.Create("Same Name", null, null);
            _mockRepository.Setup(r => r.GetByIdAsync(org.Id.ToString()))
                .ReturnsAsync(org);

            var command = new UpdateDonorOrganizationCommand
            {
                Id = org.Id.ToString(),
                Name = "Same Name",
                ContactEmail = "updated@org.com"
            };

            // Act
            var result = await _sut.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _mockRepository.Verify(r => r.GetByNameAsync(It.IsAny<string>()), Times.Never);
            _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<DonorOrganization>()), Times.Once);
        }
    }
}