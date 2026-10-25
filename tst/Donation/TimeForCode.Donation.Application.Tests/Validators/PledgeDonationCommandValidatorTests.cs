using AwesomeAssertions;
using TimeForCode.Donation.Application.Validators;
using TimeForCode.Donation.Commands;

namespace TimeForCode.Donation.Application.Tests.Validators
{
    [TestClass]
    public class PledgeDonationCommandValidatorTests
    {
        private const string OrganizationId = "5f43a0e74b12c84f1b0000d1";
        private readonly PledgeDonationCommandValidator _sut = new();

        private static PledgeDonationCommand Command(int hours = 20, bool asIndividual = true, string? organizationId = null) => new()
        {
            ProjectId = "5f43a0e74b12c84f1b000001",
            UserId = "user-1",
            Hours = hours,
            AsIndividual = asIndividual,
            DonorOrganizationId = organizationId
        };

        [TestMethod]
        public async Task Validate_IndividualPledge_ReturnsSuccess()
        {
            var result = await _sut.ValidateAsync(Command(), CancellationToken.None);

            result.IsValid.Should().BeTrue();
        }

        [TestMethod]
        public async Task Validate_OrganizationPledge_ReturnsSuccess()
        {
            var result = await _sut.ValidateAsync(Command(asIndividual: false, organizationId: OrganizationId), CancellationToken.None);

            result.IsValid.Should().BeTrue();
        }

        [TestMethod]
        public async Task Validate_IndividualAndOrganization_ReturnsFailure()
        {
            var result = await _sut.ValidateAsync(Command(asIndividual: true, organizationId: OrganizationId), CancellationToken.None);

            result.IsValid.Should().BeFalse();
        }

        [TestMethod]
        public async Task Validate_NoDonor_ReturnsFailure()
        {
            var result = await _sut.ValidateAsync(Command(asIndividual: false), CancellationToken.None);

            result.IsValid.Should().BeFalse();
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(-5)]
        [DataRow(PledgeDonationCommand.MaxHours + 1)]
        public async Task Validate_HoursOutOfRange_ReturnsFailure(int hours)
        {
            var result = await _sut.ValidateAsync(Command(hours), CancellationToken.None);

            result.IsValid.Should().BeFalse();
        }
    }
}