using FluentAssertions;
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
    }
}