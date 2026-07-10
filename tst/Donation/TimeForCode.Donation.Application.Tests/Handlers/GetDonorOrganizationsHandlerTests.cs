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
    public class GetDonorOrganizationsHandlerTests
    {
        private Mock<IDonorOrganizationRepository> _mockRepository = default!;
        private GetDonorOrganizationsHandler _sut = default!;

        [TestInitialize]
        public void Setup()
        {
            _mockRepository = new Mock<IDonorOrganizationRepository>();
            _sut = new GetDonorOrganizationsHandler(_mockRepository.Object, NullLogger<GetDonorOrganizationsHandler>.Instance);
        }

        [TestMethod]
        public async Task Handle_WithOrganizations_ReturnsPaginatedList()
        {
            // Arrange
            var orgs = new List<DonorOrganization>
            {
                DonorOrganization.Create("Org A", "a@test.com", null),
                DonorOrganization.Create("Org B", null, "https://orgb.com")
            };
            _mockRepository.Setup(r => r.GetAllAsync(1, 20))
                .ReturnsAsync((orgs as IReadOnlyList<DonorOrganization>, 2));

            var query = new GetDonorOrganizationsQuery { PageNumber = 1, PageSize = 20 };

            // Act
            var result = await _sut.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Organizations.Should().HaveCount(2);
            result.Value.TotalCount.Should().Be(2);
            result.Value.PageNumber.Should().Be(1);
            result.Value.PageSize.Should().Be(20);
        }

        [TestMethod]
        public async Task Handle_EmptyList_ReturnsEmptyResult()
        {
            // Arrange
            _mockRepository.Setup(r => r.GetAllAsync(1, 20))
                .ReturnsAsync((new List<DonorOrganization>() as IReadOnlyList<DonorOrganization>, 0));

            var query = new GetDonorOrganizationsQuery { PageNumber = 1, PageSize = 20 };

            // Act
            var result = await _sut.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Organizations.Should().BeEmpty();
            result.Value.TotalCount.Should().Be(0);
        }
    }
}