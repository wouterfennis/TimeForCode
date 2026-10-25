using AwesomeAssertions;
using TimeForCode.Authorization.Infrastructure.Services;

namespace TimeForCode.Authorization.Infrastructure.Tests.Services
{
    [TestClass]
    public class RandomGeneratorTests
    {
        private RandomGenerator _randomGenerator = default!;

        [TestInitialize]
        public void Setup()
        {
            _randomGenerator = new RandomGenerator();
        }

        [TestMethod]
        public void GenerateRandomString_ShouldReturnNonEmptyString()
        {
            // Act
            var result = _randomGenerator.GenerateRandomString();

            // Assert
            result.Should().NotBeNullOrEmpty();
        }

        [TestMethod]
        public void GenerateRandomString_ShouldReturnUniqueStrings()
        {
            // Act
            var result1 = _randomGenerator.GenerateRandomString();
            var result2 = _randomGenerator.GenerateRandomString();

            // Assert
            result1.Should().NotBe(result2);
        }

        [TestMethod]
        public void GenerateRandomString_ShouldReturnAUrlSafeStringWithAtLeast256BitsOfEntropy()
        {
            var result = _randomGenerator.GenerateRandomString();

            result.Length.Should().BeGreaterThanOrEqualTo(43);
            result.Should().MatchRegex("^[A-Za-z0-9_-]+$");
        }

        [TestMethod]
        public void GenerateCodeVerifier_ShouldReturnAnUnreservedStringOfAtLeast43Characters()
        {
            var result = _randomGenerator.GenerateCodeVerifier();

            result.Length.Should().BeInRange(43, 128);
            result.Should().MatchRegex("^[A-Za-z0-9_-]+$");
        }

        [TestMethod]
        public void GenerateCodeVerifier_ShouldReturnUniqueStrings()
        {
            _randomGenerator.GenerateCodeVerifier().Should().NotBe(_randomGenerator.GenerateCodeVerifier());
        }
    }
}