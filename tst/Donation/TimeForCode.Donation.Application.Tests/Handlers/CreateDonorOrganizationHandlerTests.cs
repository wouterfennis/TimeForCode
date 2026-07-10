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
    public class CreateDonorOrganizationHandlerTests
    {
        private Mock<IDonorOrganizationRepository> _mockRepository = default!;
        private CreateDonorOrganizationHandler _sut = default!;

        [TestInitialize]
        public void Setup()
        {
            _mockRepository = new Mock<IDonorOrganizationRepository>();
            _sut = new CreateDonorOrganizationHandler(_mockRepository.Object, NullLogger<CreateDonorOrganizationHandler>.Instance);
        }

        [TestMethod]
        public async Task Handle_ValidCommand_ReturnsSuccess()
        {
            // Arrange
            _mockRepository.Setup(r => r.GetByNameAsync(It.IsAny<string>()))
                .ReturnsAsync((DonorOrganization?)null);

            var command = new CreateDonorOrganizationCommand
            {
                Name = "Test Organization",
                ContactEmail = "test@example.com",
                Website = "https://example.com"
            };

            // Act
            var result = await _sut.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Organization.Name.Should().Be("Test Organization");
            result.Value.Organization.ContactEmail.Should().Be("test@example.com");
            result.Value.Organization.Website.Should().Be("https://example.com");
            _mockRepository.Verify(r => r.CreateAsync(It.IsAny<DonorOrganization>()), Times.Once);
        }

        [TestMethod]
        public async Task Handle_DuplicateName_ReturnsConflict()
        {
            // Arrange
            var existing = DonorOrganization.Create("Existing Org", null, null);
            _mockRepository.Setup(r => r.GetByNameAsync("Existing Org"))
                .ReturnsAsync(existing);

            var command = new CreateDonorOrganizationCommand
            {
                Name = "Existing Org"
            };

            // Act
            var result = await _sut.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.FailureStatusCode.Should().Be(System.Net.HttpStatusCode.Conflict);
            _mockRepository.Verify(r => r.CreateAsync(It.IsAny<DonorOrganization>()), Times.Never);
        }
    }
}