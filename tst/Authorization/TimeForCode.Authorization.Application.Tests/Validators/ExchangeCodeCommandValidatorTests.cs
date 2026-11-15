using AwesomeAssertions;
using TimeForCode.Authorization.Application.Validators;
using TimeForCode.Authorization.Commands;

namespace TimeForCode.Authorization.Application.Tests.Validators
{
    [TestClass]
    public class ExchangeCodeCommandValidatorTests
    {
        private readonly ExchangeCodeCommandValidator _sut = new();

        [TestMethod]
        public async Task Validate_CodeAndVerifier_ReturnsSuccess()
        {
            var result = await _sut.ValidateAsync(new ExchangeCodeCommand { Code = "code", CodeVerifier = new string('a', 43) }, CancellationToken.None);

            result.IsValid.Should().BeTrue();
        }

        [TestMethod]
        [DataRow("", "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
        [DataRow("code", "")]
        [DataRow("code", "too-short")]
        [DataRow("code", "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa!")]
        public async Task Validate_MissingOrMalformedValue_ReturnsFailure(string code, string verifier)
        {
            var result = await _sut.ValidateAsync(new ExchangeCodeCommand { Code = code, CodeVerifier = verifier }, CancellationToken.None);

            result.IsValid.Should().BeFalse();
        }
    }
}