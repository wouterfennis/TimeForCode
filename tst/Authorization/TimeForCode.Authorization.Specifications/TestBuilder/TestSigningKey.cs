using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace TimeForCode.Authorization.Specifications.TestBuilder
{
    /// <summary>
    /// A signing key the test holds itself: the certificate to configure and the key to sign tokens with.
    /// </summary>
    internal sealed class TestSigningKey : IDisposable
    {
        private readonly RSA _rsa = RSA.Create(2048);

        public TestSigningKey()
        {
            var request = new CertificateRequest("CN=test", _rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(30));
            Base64Certificate = Convert.ToBase64String(certificate.Export(X509ContentType.Pfx));
            Modulus = Base64UrlEncoder.Encode(_rsa.ExportParameters(includePrivateParameters: false).Modulus);
        }

        public string Base64Certificate { get; }

        /// <summary>
        /// The public modulus as published in the key set.
        /// </summary>
        public string Modulus { get; }

        public SigningCredentials CredentialsWithKeyId(string keyId) =>
            new(new RsaSecurityKey(_rsa) { KeyId = keyId }, SecurityAlgorithms.RsaSha256);

        public void Dispose() => _rsa.Dispose();
    }
}