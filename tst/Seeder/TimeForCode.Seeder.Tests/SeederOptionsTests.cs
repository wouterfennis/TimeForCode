using AwesomeAssertions;
using TimeForCode.Seeder;

namespace TimeForCode.Seeder.Tests
{
    [TestClass]
    public class SeederOptionsTests
    {
        private static readonly Dictionary<string, string?> SecretOnly = new() { ["TIMEFORCODE_LIVE_TEST_ADMIN_SECRET"] = "s3cret" };

        private static string? Lookup(Dictionary<string, string?> env, string name) => env.GetValueOrDefault(name);

        [TestMethod]
        public void Resolve_OnlySecret_UsesTheLocalStackDefaults()
        {
            SeederOptions options = SeederOptions.Resolve([], name => Lookup(SecretOnly, name));

            options.AuthorizationUrl.Should().Be(new Uri("http://localhost:8080"));
            options.DonationUrl.Should().Be(new Uri("http://localhost:8082"));
            options.ClientRedirectUri.Should().Be(new Uri("http://localhost:8084"));
            options.AdminSecret.Should().Be("s3cret");
        }

        [TestMethod]
        public void Resolve_NoSecret_FailsNamingTheOptionAndTheVariable()
        {
            Action act = () => SeederOptions.Resolve([], _ => null);

            act.Should().Throw<SeederException>().WithMessage("*--admin-secret*TIMEFORCODE_LIVE_TEST_ADMIN_SECRET*");
        }

        [TestMethod]
        public void Resolve_Environment_OverridesTheDefaults()
        {
            var env = new Dictionary<string, string?>(SecretOnly) { ["TIMEFORCODE_LIVE_DONATION_URL"] = "http://donation:9000" };

            SeederOptions.Resolve([], name => Lookup(env, name)).DonationUrl.Should().Be(new Uri("http://donation:9000"));
        }

        [TestMethod]
        public void Resolve_Arguments_OverrideTheEnvironment()
        {
            var env = new Dictionary<string, string?>(SecretOnly) { ["TIMEFORCODE_LIVE_DONATION_URL"] = "http://donation:9000" };

            SeederOptions options = SeederOptions.Resolve(
                ["--donation-url", "http://other:1", "--authorization-url", "http://auth:2", "--admin-secret", "cli"],
                name => Lookup(env, name));

            options.DonationUrl.Should().Be(new Uri("http://other:1"));
            options.AuthorizationUrl.Should().Be(new Uri("http://auth:2"));
            options.AdminSecret.Should().Be("cli");
        }

        [TestMethod]
        public void Resolve_RelativeUrl_FailsNamingTheOption()
        {
            Action act = () => SeederOptions.Resolve(["--donation-url", "not a url"], name => Lookup(SecretOnly, name));

            act.Should().Throw<SeederException>().WithMessage("*--donation-url*");
        }

        [TestMethod]
        public void Resolve_UnknownOption_Fails()
        {
            Action act = () => SeederOptions.Resolve(["--bogus", "x"], name => Lookup(SecretOnly, name));

            act.Should().Throw<SeederException>().WithMessage("*--bogus*");
        }

        [TestMethod]
        public void Resolve_OptionWithoutValue_Fails()
        {
            Action act = () => SeederOptions.Resolve(["--donation-url"], name => Lookup(SecretOnly, name));

            act.Should().Throw<SeederException>().WithMessage("*--donation-url*value*");
        }
    }
}