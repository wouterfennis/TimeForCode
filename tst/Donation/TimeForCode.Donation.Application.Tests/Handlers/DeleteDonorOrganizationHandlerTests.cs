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
    public class DeleteDonorOrganizationHandlerTests
    {
        private Mock<IDonorOrganizationRepository> _mockRepository = default!;
        private DeleteDonorOrganizationHandler _sut = default!;

        [TestInitialize]
        public void Setup()
        {
            _mockRepository = new Mock<IDonorOrganizationRepository>();
            _sut = new DeleteDonorOrganizationHandler(_mockRepository.Object, NullLogger<DeleteDonorOrganizationHandler>.Instance);
        }

        [TestMethod]
        public async Task Handle_ExistingId_ReturnsSuccess()
        {
            // Arrange
            var org = DonorOrganization.Create("To Delete", null, null);
            _mockRepository.Setup(r => r.GetByIdAsync(org.Id.ToString()))
                .ReturnsAsync(org);

            var command = new DeleteDonorOrganizationCommand { Id = org.Id.ToString() };

            // Act
            var result = await _sut.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _mockRepository.Verify(r => r.DeleteAsync(org.Id.ToString()), Times.Once);
        }

        [TestMethod]
        public async Task Handle_NonExistingId_ReturnsFailure()
        {
            // Arrange
            _mockRepository.Setup(r => r.GetByIdAsync("nonexistent"))
                .ReturnsAsync((DonorOrganization?)null);

            var command = new DeleteDonorOrganizationCommand { Id = "nonexistent" };

            // Act
            var result = await _sut.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.ErrorMessage.Should().Contain("nonexistent");
            _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<string>()), Times.Never);
        }
    }
}