using Microsoft.AspNetCore.DataProtection;
using TimeForCode.Authorization.Application.Interfaces;

namespace TimeForCode.Authorization.Infrastructure.Services
{
    internal class DataProtectionEncryptionService : IEncryptionService
    {
        private const string Purpose = "GitHubAccessToken";
        private readonly IDataProtector _protector;

        public DataProtectionEncryptionService(IDataProtectionProvider provider)
        {
            _protector = provider.CreateProtector(Purpose);
        }

        public string Encrypt(string plaintext)
        {
            return _protector.Protect(plaintext);
        }

        public string Decrypt(string ciphertext)
        {
            return _protector.Unprotect(ciphertext);
        }
    }
}