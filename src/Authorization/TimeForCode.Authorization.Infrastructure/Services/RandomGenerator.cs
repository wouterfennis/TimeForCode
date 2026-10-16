using System.Buffers.Text;
using System.Security.Cryptography;
using TimeForCode.Authorization.Application.Interfaces;

namespace TimeForCode.Authorization.Infrastructure.Services
{
    public class RandomGenerator : IRandomGenerator
    {
        public string GenerateRandomString()
        {
            return Guid.NewGuid().ToString("N");
        }

        public string GenerateCodeVerifier()
        {
            return Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(32));
        }
    }
}