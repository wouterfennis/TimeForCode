using TimeForCode.Authorization.Commands;

namespace TimeForCode.Authorization.Application.Interfaces
{
    public interface IRandomGenerator
    {
        public string GenerateRandomString();
    }
}