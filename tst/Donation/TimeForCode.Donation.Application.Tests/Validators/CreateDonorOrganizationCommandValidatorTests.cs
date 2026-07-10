using FluentAssertions;
using TimeForCode.Donation.Application.Validators;
using TimeForCode.Donation.Commands;

namespace TimeForCode.Donation.Application.Tests.Validators
{
    [TestClass]
    public class CreateDonorOrganizationCommandValidatorTests
    {
        private readonly CreateDonorOrganizationCommandValidator _sut = new();

        [TestMethod]
        public async Task Validate_ValidCommand_ReturnsSuccess()
        {
            var command = new CreateDonorOrganizationCommand
            {
                Name = "Valid Organization",
                ContactEmail = "valid@example.com",
                Website = "https://example.com"
            };

            var result = await _sut.ValidateAsync(command, CancellationToken.None);

            result.IsValid.Should().BeTrue();
        }

        [TestMethod]
        public async Task Validate_EmptyName_ReturnsFailure()
        {
            var command = new CreateDonorOrganizationCommand
            {
                Name = ""
            };

            var result = await _sut.ValidateAsync(command, CancellationToken.None);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "Name");
        }

        [TestMethod]
        public async Task Validate_NameTooLong_ReturnsFailure()
        {
            var command = new CreateDonorOrganizationCommand
            {
                Name = new string('A', 201)
            };

            var result = await _sut.ValidateAsync(command, CancellationToken.None);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "Name");
        }

        [TestMethod]
        public async Task Validate_InvalidEmail_ReturnsFailure()
        {
            var command = new CreateDonorOrganizationCommand
            {
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
            var command = new CreateDonorOrganizationCommand
            {
                Name = "Valid Org",
                Website = "not-a-url"
            };

            var result = await _sut.ValidateAsync(command, CancellationToken.None);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "Website");
        }

        [TestMethod]
        public async Task Validate_NullOptionalFields_ReturnsSuccess()
        {
            var command = new CreateDonorOrganizationCommand
            {
                Name = "Valid Org",
                ContactEmail = null,
                Website = null
            };

            var result = await _sut.ValidateAsync(command, CancellationToken.None);

            result.IsValid.Should().BeTrue();
        }
    }
}