using Microsoft.Extensions.Caching.Memory;
using TimeForCode.Authorization.Application.Interfaces;
using TimeForCode.Authorization.Domain.Entities;

namespace TimeForCode.Authorization.Infrastructure.Persistence.Memory
{
    public class StateRepository : IStateRepository
    {
        private readonly IMemoryCache _memoryCache;

        public StateRepository(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
        }

        public void AddState(StateEntry stateEntry)
        {
            _memoryCache.Set(stateEntry.Key, stateEntry, TimeSpan.FromMinutes(10));
        }

        public StateEntry? GetState(string state)
        {
            var stateEntry = _memoryCache.Get<StateEntry>(state);

            return stateEntry;
        }
    }
}
