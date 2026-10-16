using System.Collections.Concurrent;
using TimeForCode.Authorization.Application.Interfaces;
using TimeForCode.Authorization.Domain.Entities;

namespace TimeForCode.Authorization.Specifications.Mocking
{
    /// <summary>
    /// Stands in for the shared database: instances created from the same fake see the same login states.
    /// </summary>
    public class InMemoryStateRepository : IStateRepository
    {
        private readonly ConcurrentDictionary<string, StateEntry> _entries = new();

        public Task AddAsync(StateEntry stateEntry)
        {
            _entries[stateEntry.Key] = stateEntry;
            return Task.CompletedTask;
        }

        public Task<StateEntry?> TakeAsync(string key)
        {
            _entries.TryRemove(key, out StateEntry? entry);
            return Task.FromResult(entry);
        }
    }
}