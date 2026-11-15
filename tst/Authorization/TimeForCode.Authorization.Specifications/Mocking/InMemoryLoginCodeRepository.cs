using System.Collections.Concurrent;
using TimeForCode.Authorization.Application.Interfaces;
using TimeForCode.Authorization.Domain.Entities;

namespace TimeForCode.Authorization.Specifications.Mocking
{
    /// <summary>
    /// Stands in for the shared database: instances created from the same fake see the same single-use login codes.
    /// </summary>
    public class InMemoryLoginCodeRepository : ILoginCodeRepository
    {
        private readonly ConcurrentDictionary<string, LoginCode> _codes = new();

        public Task AddAsync(LoginCode loginCode)
        {
            _codes[loginCode.CodeHash] = loginCode;
            return Task.CompletedTask;
        }

        public Task<LoginCode?> TakeAsync(string codeHash)
        {
            _codes.TryRemove(codeHash, out LoginCode? code);
            return Task.FromResult(code);
        }
    }
}