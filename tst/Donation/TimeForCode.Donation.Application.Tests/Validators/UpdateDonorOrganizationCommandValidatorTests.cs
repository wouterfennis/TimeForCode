using FluentAssertions;
using TimeForCode.Donation.Application.Validators;
using TimeForCode.Donation.Commands;

namespace TimeForCode.Donation.Application.Tests.Validators
{
    [TestClass]
    public class UpdateDonorOrganizationCommandValidatorTests
    {
        private readonly UpdateDonorOrganizationCommandValidator _sut = new();

        [TestMethod]
        public async Task Validate_ValidCommand_ReturnsSuccess()
        {
            var command = new UpdateDonorOrganizationCommand
            {
                Id = "507f1f77bcf86cd799439011",
                Name = "Valid Organization",
                ContactEmail = "valid@example.com",
                Website = "https://example.com"
            };

            var result = await _sut.ValidateAsync(command, CancellationToken.None);

            result.IsValid.Should().BeTrue();
        }

        [TestMethod]
        public async Task Validate_EmptyId_ReturnsFailure()
        {
            var command = new UpdateDonorOrganizationCommand
            {
                Id = "",
                Name = "Valid Org"
            };

            var result = await _sut.ValidateAsync(command, CancellationToken.None);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "Id");
        }

        [TestMethod]
        public async Task Validate_EmptyName_ReturnsFailure()
        {
            var command = new UpdateDonorOrganizationCommand
            {
                Id = "507f1f77bcf86cd799439011",
                Name = ""
            };

            var result = await _sut.ValidateAsync(command, CancellationToken.None);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "Name");
        }

        [TestMethod]
        public async Task Validate_InvalidEmail_ReturnsFailure()
        {
            var command = new UpdateDonorOrganizationCommand
            {
                Id = "507f1f77bcf86cd799439011",
                Name = "Valid Org",
                ContactEmail = "not-an-email"
            };

            var result = await _sut.ValidateAsync(command, CancellationToken.None);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "ContactEmail");
        }

        [TestMethod]
        public async Task Validate_InvalidWebsite_ReturnsFailure()
        {
            var command = new UpdateDonorOrganizationCommand
            {
                Id = "507f1f77bcf86cd799439011",
                Name = "Valid Org",
                Website = "not-a-url"
            };

            var result = await _sut.ValidateAsync(command, CancellationToken.None);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "Website");
        }
    }
}