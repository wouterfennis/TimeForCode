using AwesomeAssertions;
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
    public class GetDonorOrganizationsHandlerTests
    {
        private Mock<IDonorOrganizationRepository> _mockRepository = default!;
        private Mock<IDonationRepository> _mockDonationRepository = default!;
        private GetDonorOrganizationsHandler _sut = default!;

        [TestInitialize]
        public void Setup()
        {
            _mockRepository = new Mock<IDonorOrganizationRepository>();
            _mockDonationRepository = new Mock<IDonationRepository>();
            _mockDonationRepository.Setup(r => r.GetByDonorOrganizationIdsAsync(It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
            _sut = new GetDonorOrganizationsHandler(_mockRepository.Object, _mockDonationRepository.Object, NullLogger<GetDonorOrganizationsHandler>.Instance);
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
        public async Task Handle_OrganizationWithDonation_ReturnsItsHours()
        {
            var org = DonorOrganization.Create("Org A", null, null);
            _mockRepository.Setup(r => r.GetAllAsync(1, 20))
                .ReturnsAsync(((IReadOnlyList<DonorOrganization>)new List<DonorOrganization> { org }, 1));
            _mockDonationRepository.Setup(r => r.GetByDonorOrganizationIdsAsync(It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([DonationTestData.Build(DonationStatus.Active, committed: 10, logged: 4, organizationId: org.Id)]);

            var result = await _sut.Handle(new GetDonorOrganizationsQuery { PageNumber = 1, PageSize = 20 }, CancellationToken.None);

            result.Value.Organizations.Single().HoursSpent.Should().Be(4);
            result.Value.Organizations.Single().HoursAvailable.Should().Be(6);
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