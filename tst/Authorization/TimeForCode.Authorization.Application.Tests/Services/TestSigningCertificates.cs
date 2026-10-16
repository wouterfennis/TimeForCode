using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace TimeForCode.Authorization.Application.Tests.Services
{
    internal static class TestSigningCertificates
    {
        public static string CreateBase64Pfx()
        {
            using var rsa = RSA.Create(2048);
            var request = new CertificateRequest("CN=test", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(30));
            return Convert.ToBase64String(certificate.Export(X509ContentType.Pfx));
        }

        public static string CreateBase64PfxWithoutPrivateKey()
        {
            using var rsa = RSA.Create(2048);
            var request = new CertificateRequest("CN=test", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(30));
            using var publicOnly = X509CertificateLoader.LoadCertificate(certificate.Export(X509ContentType.Cert));
            return Convert.ToBase64String(publicOnly.Export(X509ContentType.Pfx));
        }
    }
}