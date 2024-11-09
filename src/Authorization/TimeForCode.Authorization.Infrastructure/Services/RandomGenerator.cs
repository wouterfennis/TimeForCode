using TimeForCode.Authorization.Application.Interfaces;

namespace TimeForCode.Authorization.Infrastructure.Services
{
    public class RandomGenerator : IRandomGenerator
    {
        public string GenerateRandomString()
        {
            return Guid.NewGuid().ToString("N");
        }
    }
}