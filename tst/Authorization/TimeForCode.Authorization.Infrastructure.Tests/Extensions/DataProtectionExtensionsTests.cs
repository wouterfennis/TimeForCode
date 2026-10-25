using AwesomeAssertions;
using TimeForCode.Authorization.Infrastructure.Extensions;

namespace TimeForCode.Authorization.Infrastructure.Tests.Extensions
{
    [TestClass]
    public class DataProtectionExtensionsTests
    {
        [TestMethod]
        public void UseSharedKeyRing_ConnectionStringConfigured_ReturnsTrue()
        {
            DataProtectionExtensions.UseSharedKeyRing("mongodb://mongo:27017", "TimeForCode.Authorization.Api")
                .Should().BeTrue();
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("  ")]
        public void UseSharedKeyRing_NoConnectionString_ReturnsFalse(string? connectionString)
        {
            DataProtectionExtensions.UseSharedKeyRing(connectionString, "TimeForCode.Authorization.Api")
                .Should().BeFalse();
        }

        [TestMethod]
        public void UseSharedKeyRing_OpenApiDocumentGeneration_ReturnsFalseDespiteConnectionString()
        {
            DataProtectionExtensions.UseSharedKeyRing("mongodb://localhost:27017", "GetDocument.Insider")
                .Should().BeFalse();
        }
    }
}