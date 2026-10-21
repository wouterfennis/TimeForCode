using FluentAssertions;
using Microsoft.Extensions.Configuration;
using TimeForCode.Authorization.Application.Options;

namespace TimeForCode.Authorization.Application.Tests.Options
{
    [TestClass]
    public class TestAdminAccessOptionsTests
    {
        private static TestAdminAccessOptions Options(bool enabled, string secret = "a-long-enough-test-secret") => new()
        {
            Enabled = enabled,
            Secret = secret
        };

        [TestMethod]
        public void Bind_NoSection_IsDisabled()
        {
            var configuration = new ConfigurationBuilder().Build();

            var result = TestAdminAccessOptions.Bind(configuration);

            result.Enabled.Should().BeFalse();
        }

        [TestMethod]
        public void Bind_EmptyValues_IsDisabled()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["TestAdminAccess:Enabled"] = "",
                    ["TestAdminAccess:Secret"] = ""
                })
                .Build();

            var result = TestAdminAccessOptions.Bind(configuration);

            result.Enabled.Should().BeFalse();
        }

        [TestMethod]
        public void Bind_EnabledSection_ReadsValues()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["TestAdminAccess:Enabled"] = "true",
                    ["TestAdminAccess:Secret"] = "a-long-enough-test-secret"
                })
                .Build();

            var result = TestAdminAccessOptions.Bind(configuration);

            result.Enabled.Should().BeTrue();
            result.Secret.Should().Be("a-long-enough-test-secret");
        }

        [TestMethod]
        [DataRow("Production")]
        [DataRow("Staging")]
        [DataRow("")]
        [DataRow("Something-Else")]
        public void EnsureSafeFor_EnabledOutsideDevelopmentAndTest_Throws(string environmentName)
        {
            var act = () => Options(enabled: true).EnsureSafeFor(environmentName);

            act.Should().Throw<InvalidOperationException>();
        }

        [TestMethod]
        [DataRow("Development")]
        [DataRow("Test")]
        public void EnsureSafeFor_EnabledInDevelopmentOrTest_DoesNotThrow(string environmentName)
        {
            var act = () => Options(enabled: true).EnsureSafeFor(environmentName);

            act.Should().NotThrow();
        }

        [TestMethod]
        [DataRow("Production")]
        [DataRow("Development")]
        public void EnsureSafeFor_Disabled_DoesNotThrow(string environmentName)
        {
            var act = () => Options(enabled: false, secret: "").EnsureSafeFor(environmentName);

            act.Should().NotThrow();
        }

        [TestMethod]
        [DataRow("")]
        [DataRow("too-short")]
        public void EnsureSafeFor_EnabledWithWeakSecret_Throws(string secret)
        {
            var act = () => Options(enabled: true, secret).EnsureSafeFor("Development");

            act.Should().Throw<InvalidOperationException>();
        }
    }
}