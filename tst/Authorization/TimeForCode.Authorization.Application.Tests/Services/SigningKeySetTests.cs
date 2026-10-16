using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using TimeForCode.Authorization.Application.Services;

namespace TimeForCode.Authorization.Application.Tests.Services
{
    [TestClass]
    public class SigningKeySetTests
    {
        private static IConfiguration Configuration(string? current, string? previous = null)
        {
            var values = new Dictionary<string, string?>
            {
                ["RsaOptions:Base64Certificate"] = current,
                ["RsaOptions:PreviousBase64Certificate"] = previous
            };
            return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("   ")]
        public void Load_WithoutCertificateOutsideDevelopment_Throws(string? certificate)
        {
            var act = () => SigningKeySet.Load(Configuration(certificate), allowEphemeralKey: false);

            act.Should().Throw<InvalidOperationException>().WithMessage("*signing certificate is required*");
        }

        [TestMethod]
        public void Load_WithoutCertificateInDevelopment_CreatesEphemeralKeyWithKeyId()
        {
            var set = SigningKeySet.Load(Configuration(""), allowEphemeralKey: true);

            set.Current.KeyId.Should().NotBeNullOrEmpty();
            set.Previous.Should().BeNull();
            set.PublishedKeys.Should().ContainSingle();
        }

        [TestMethod]
        public void Load_WithCertificate_UsesStableKeyIdDerivedFromThePublicKey()
        {
            var certificate = TestSigningCertificates.CreateBase64Pfx();

            var first = SigningKeySet.Load(Configuration(certificate), allowEphemeralKey: false);
            var second = SigningKeySet.Load(Configuration(certificate), allowEphemeralKey: false);

            first.Current.KeyId.Should().Be(second.Current.KeyId);
        }

        [TestMethod]
        public void Load_WithPreviousCertificate_PublishesCurrentAndPreviousWithDistinctKeyIds()
        {
            var set = SigningKeySet.Load(
                Configuration(TestSigningCertificates.CreateBase64Pfx(), TestSigningCertificates.CreateBase64Pfx()),
                allowEphemeralKey: false);

            set.Previous.Should().NotBeNull();
            set.PublishedKeys.Should().HaveCount(2);
            set.Current.KeyId.Should().NotBe(set.Previous!.KeyId);
        }

        [TestMethod]
        public void Load_WithMalformedCertificate_ThrowsNamingTheSetting()
        {
            var act = () => SigningKeySet.Load(Configuration("not-base64!"), allowEphemeralKey: false);

            act.Should().Throw<InvalidOperationException>().WithMessage("*Base64Certificate*");
        }

        [TestMethod]
        public void Load_WithMalformedPreviousCertificate_ThrowsNamingTheSetting()
        {
            var act = () => SigningKeySet.Load(
                Configuration(TestSigningCertificates.CreateBase64Pfx(), "not-base64!"),
                allowEphemeralKey: false);

            act.Should().Throw<InvalidOperationException>().WithMessage("*PreviousBase64Certificate*");
        }

        [TestMethod]
        public void Load_WithCertificateWithoutPrivateKey_ThrowsNamingTheSetting()
        {
            var act = () => SigningKeySet.Load(
                Configuration(TestSigningCertificates.CreateBase64PfxWithoutPrivateKey()),
                allowEphemeralKey: false);

            act.Should().Throw<InvalidOperationException>().WithMessage("*private key*");
        }

        [TestMethod]
        public void Load_WithIdenticalCurrentAndPreviousCertificate_Throws()
        {
            var certificate = TestSigningCertificates.CreateBase64Pfx();

            var act = () => SigningKeySet.Load(Configuration(certificate, certificate), allowEphemeralKey: false);

            act.Should().Throw<InvalidOperationException>().WithMessage("*same key*");
        }

        [TestMethod]
        public void Algorithm_IsRs256AndUseIsSignature()
        {
            SigningKeySet.Algorithm.Should().Be(SecurityAlgorithms.RsaSha256);
            SigningKeySet.Use.Should().Be("sig");
        }
    }
}