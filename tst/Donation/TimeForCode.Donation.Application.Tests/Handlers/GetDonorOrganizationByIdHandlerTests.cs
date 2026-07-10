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
    public class GetDonorOrganizationByIdHandlerTests
    {
        private Mock<IDonorOrganizationRepository> _mockRepository = default!;
        private GetDonorOrganizationByIdHandler _sut = default!;

        [TestInitialize]
        public void Setup()
        {
            _mockRepository = new Mock<IDonorOrganizationRepository>();
            _sut = new GetDonorOrganizationByIdHandler(_mockRepository.Object, NullLogger<GetDonorOrganizationByIdHandler>.Instance);
        }

        [TestMethod]
        public async Task Handle_ExistingId_ReturnsSuccess()
        {
            // Arrange
            var org = DonorOrganization.Create("Test Org", "test@org.com", "https://test.org");
            _mockRepository.Setup(r => r.GetByIdAsync(org.Id.ToString()))
                .ReturnsAsync(org);

            var query = new GetDonorOrganizationByIdQuery { Id = org.Id.ToString() };

            // Act
            var result = await _sut.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Organization.Name.Should().Be("Test Org");
            result.Value.Organization.ContactEmail.Should().Be("test@org.com");
            result.Value.Organization.Website.Should().Be("https://test.org");
        }

        [TestMethod]
        public async Task Handle_NonExistingId_ReturnsFailure()
        {
            // Arrange
            _mockRepository.Setup(r => r.GetByIdAsync("nonexistent"))
                .ReturnsAsync((DonorOrganization?)null);

            var query = new GetDonorOrganizationByIdQuery { Id = "nonexistent" };

            // Act
            var result = await _sut.Handle(query, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.ErrorMessage.Should().Contain("nonexistent");
        }
    }
}