using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using TimeForCode.Authorization.Application.Options;

namespace TimeForCode.Authorization.Application.Services
{
    /// <summary>
    /// The keys the platform signs with (current) and still publishes for validation (previous).
    /// </summary>
    public sealed class SigningKeySet
    {
        public const string Algorithm = SecurityAlgorithms.RsaSha256;
        public const string Use = "sig";

        public SigningKeySet(RSA current, RSA? previous = null)
        {
            Current = CreateKey(current);
            Previous = previous == null ? null : CreateKey(previous);

            if (Previous != null && Previous.KeyId == Current.KeyId)
            {
                throw new InvalidOperationException(
                    $"'{RsaOptions.SectionName}:{nameof(RsaOptions.PreviousBase64Certificate)}' holds the same key as the current certificate.");
            }
        }

        public RsaSecurityKey Current { get; }

        public RsaSecurityKey? Previous { get; }

        public IReadOnlyList<RsaSecurityKey> PublishedKeys =>
            Previous == null ? [Current] : [Current, Previous];

        public static SigningKeySet Load(IConfiguration configuration, bool allowEphemeralKey)
        {
            var rsaOptions = RsaOptions.Bind(configuration);

            RSA current;
            if (string.IsNullOrWhiteSpace(rsaOptions.Base64Certificate))
            {
                if (!allowEphemeralKey)
                {
                    throw new InvalidOperationException(
                        $"A signing certificate is required outside the Development environment. Configure '{RsaOptions.SectionName}:{nameof(RsaOptions.Base64Certificate)}'.");
                }

                current = RSA.Create(2048);
            }
            else
            {
                current = LoadPrivateKey(rsaOptions.Base64Certificate, nameof(RsaOptions.Base64Certificate));
            }

            var previous = string.IsNullOrWhiteSpace(rsaOptions.PreviousBase64Certificate)
                ? null
                : LoadPrivateKey(rsaOptions.PreviousBase64Certificate, nameof(RsaOptions.PreviousBase64Certificate));

            return new SigningKeySet(current, previous);
        }

        private static RSA LoadPrivateKey(string base64Certificate, string settingName)
        {
            try
            {
                using var certificate = X509CertificateLoader.LoadPkcs12(Convert.FromBase64String(base64Certificate), (string?)null);
                return certificate.GetRSAPrivateKey()
                    ?? throw new InvalidOperationException(
                        $"'{RsaOptions.SectionName}:{settingName}' holds a certificate without an RSA private key.");
            }
            catch (Exception ex) when (ex is FormatException or CryptographicException)
            {
                throw new InvalidOperationException(
                    $"'{RsaOptions.SectionName}:{settingName}' is not a valid base64 encoded PKCS#12 certificate.", ex);
            }
        }

        private static RsaSecurityKey CreateKey(RSA rsa)
        {
            var keyId = Base64UrlEncoder.Encode(SHA256.HashData(rsa.ExportSubjectPublicKeyInfo()));
            return new RsaSecurityKey(rsa) { KeyId = keyId };
        }
    }
}