using FluentAssertions;
using TimeForCode.Authorization.Application.Validators;
using TimeForCode.Authorization.Commands;
using TimeForCode.Authorization.Values;

namespace TimeForCode.Authorization.Application.Tests.Validators
{
    [TestClass]
    public class RefreshCommandValidatorTests
    {
        private readonly RefreshCommandValidator _sut = new();

        [TestMethod]
        public async Task Validate_ValidCommand_ReturnsSuccess()
        {
            var command = new RefreshCommand
            {
                RefreshToken = new RefreshToken
                {
                    Token = "valid-token",
                    ExpiresAfter = DateTimeOffset.UtcNow.AddDays(1)
                }
            };

            var result = await _sut.ValidateAsync(command);

            result.IsValid.Should().BeTrue();
        }

        [TestMethod]
        public async Task Validate_EmptyTokenValue_ReturnsFailure()
        {
            var command = new RefreshCommand
            {
                RefreshToken = new RefreshToken
                {
                    Token = string.Empty,
                    ExpiresAfter = DateTimeOffset.UtcNow.AddDays(1)
                }
            };

            var result = await _sut.ValidateAsync(command);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName.Contains("Token"));
        }
    }
}