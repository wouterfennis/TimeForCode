using FluentAssertions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Security.Cryptography;
using TimeForCode.Authorization.Infrastructure.Extensions;
using TimeForCode.Authorization.Infrastructure.Services;

namespace TimeForCode.Authorization.Infrastructure.Tests.Services
{
    [TestClass]
    public class DataProtectionEncryptionServiceTests
    {
        [TestMethod]
        public void Decrypt_StateEncryptedByAnotherInstanceSharingTheKeyStore_ReturnsPlaintext()
        {
            var sharedKeyStore = new InMemoryXmlRepository();
            var instanceA = CreateService(sharedKeyStore);
            var instanceB = CreateService(sharedKeyStore);

            var ciphertext = instanceA.Encrypt("ceremony-state");

            instanceB.Decrypt(ciphertext).Should().Be("ceremony-state");
        }

        [TestMethod]
        public void Decrypt_StateEncryptedWithAnotherKeyStore_ThrowsCryptographicException()
        {
            var instanceA = CreateService(new InMemoryXmlRepository());
            var instanceB = CreateService(new InMemoryXmlRepository());

            var ciphertext = instanceA.Encrypt("ceremony-state");

            Action act = () => instanceB.Decrypt(ciphertext);

            act.Should().Throw<CryptographicException>();
        }

        private static DataProtectionEncryptionService CreateService(IXmlRepository keyStore)
        {
            var services = new ServiceCollection();
            services.AddSharedKeyRingDataProtection();
            services.Replace(ServiceDescriptor.Singleton(keyStore));

            var provider = services.BuildServiceProvider();
            return new DataProtectionEncryptionService(provider.GetRequiredService<IDataProtectionProvider>());
        }
    }
}