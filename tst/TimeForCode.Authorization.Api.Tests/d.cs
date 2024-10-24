using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace TimeForCode.Authorization.Api.Tests
{
    [TestClass]
    public class d
    {
        // generate RSA key pair
        [TestMethod]
        public void GenerateRsaKeyPair()
        {
            using var rsa = RSA.Create(2048);
            var request = new CertificateRequest(
                "CN=IdentityProviderMockService",
                rsa,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

            var certificate = request.CreateSelfSigned(
                DateTimeOffset.Now,
                DateTimeOffset.Now.AddYears(5));

            // Export the certificate including the private key
            var certificateBytes = certificate.Export(X509ContentType.Pfx, (string?)null);

            Console.WriteLine($"Exponent:  {Convert.ToBase64String(certificateBytes)}");
        }
    }
}
