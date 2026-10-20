using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TimeForCode.Donation.Application.Handlers;
using TimeForCode.Donation.Application.Interfaces;
using TimeForCode.Donation.Commands;
using TimeForCode.Donation.Domain;
using TimeForCode.Donation.Values;

namespace TimeForCode.Donation.Application.Tests.Handlers
{
    [TestClass]
    public class GetDonorOrganizationByIdHandlerTests
    {
        private Mock<IDonorOrganizationRepository> _mockRepository = default!;
        private Mock<IDonationRepository> _mockDonationRepository = default!;
        private GetDonorOrganizationByIdHandler _sut = default!;

        [TestInitialize]
        public void Setup()
        {
            _mockRepository = new Mock<IDonorOrganizationRepository>();
            _mockDonationRepository = new Mock<IDonationRepository>();
            _mockDonationRepository.Setup(r => r.GetByDonorOrganizationIdsAsync(It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
            _sut = new GetDonorOrganizationByIdHandler(_mockRepository.Object, _mockDonationRepository.Object, NullLogger<GetDonorOrganizationByIdHandler>.Instance);
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
        public async Task Handle_OrganizationWithDonations_ReturnsHoursSpentAndAvailable()
        {
            var org = DonorOrganization.Create("Test Org", null, null);
            _mockRepository.Setup(r => r.GetByIdAsync(org.Id.ToString())).ReturnsAsync(org);
            _mockDonationRepository.Setup(r => r.GetByDonorOrganizationIdsAsync(
                    It.Is<IReadOnlyCollection<string>>(ids => ids.Contains(org.Id.ToString())), It.IsAny<CancellationToken>()))
                .ReturnsAsync([
                    DonationTestData.Build(DonationStatus.Active, committed: 10, logged: 4, organizationId: org.Id),
                    DonationTestData.Build(DonationStatus.Completed, committed: 5, logged: 5, organizationId: org.Id),
                    DonationTestData.Build(DonationStatus.Pending, committed: 8, organizationId: org.Id),
                    DonationTestData.Build(DonationStatus.Cancelled, committed: 7, logged: 2, organizationId: org.Id)
                ]);

            var result = await _sut.Handle(new GetDonorOrganizationByIdQuery { Id = org.Id.ToString() }, CancellationToken.None);

            result.Value.Organization.HoursSpent.Should().Be(11);
            result.Value.Organization.HoursAvailable.Should().Be(6);
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