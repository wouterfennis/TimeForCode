using AwesomeAssertions;
using TimeForCode.Authorization.Application.Validators;
using TimeForCode.Authorization.Commands;
using TimeForCode.Authorization.Values;

namespace TimeForCode.Authorization.Application.Tests.Validators
{
    [TestClass]
    public class LoginCommandValidatorTests
    {
        private readonly LoginCommandValidator _sut = new();

        [TestMethod]
        public async Task Validate_ValidCommand_ReturnsSuccess()
        {
            var command = new LoginCommand
            {
                IdentityProvider = IdentityProvider.Github,
                RedirectUri = new Uri("https://example.com/callback"),
                CodeChallenge = new string('a', 43)
            };

            var result = await _sut.ValidateAsync(command, CancellationToken.None);

            result.IsValid.Should().BeTrue();
        }

        [TestMethod]
        public async Task Validate_InvalidIdentityProvider_ReturnsFailure()
        {
            var command = new LoginCommand
            {
                IdentityProvider = (IdentityProvider)999,
                RedirectUri = new Uri("https://example.com/callback"),
                CodeChallenge = new string('a', 43)
            };

            var result = await _sut.ValidateAsync(command, CancellationToken.None);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(LoginCommand.IdentityProvider));
        }

        [TestMethod]
        [DataRow("")]
        [DataRow("too-short")]
        [DataRow("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa=")]
        [DataRow("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
        public async Task Validate_MissingOrMalformedCodeChallenge_ReturnsFailure(string challenge)
        {
            var command = new LoginCommand
            {
                IdentityProvider = IdentityProvider.Github,
                RedirectUri = new Uri("https://example.com/callback"),
                CodeChallenge = challenge
            };

            var result = await _sut.ValidateAsync(command, CancellationToken.None);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(LoginCommand.CodeChallenge));
        }
    }
}