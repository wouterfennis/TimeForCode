using System.Collections.Concurrent;
using TimeForCode.Authorization.Application.Interfaces.Admin;
using TimeForCode.Authorization.Domain.Entities;

namespace TimeForCode.Authorization.Infrastructure.Tests.Services
{
    /// <summary>The ceremony state repository without a database: taking an entry removes it, like the atomic delete in MongoDB.</summary>
    internal sealed class InMemoryCeremonyStateRepository : ICeremonyStateRepository
    {
        public ConcurrentDictionary<string, CeremonyState> Entries { get; } = new();

        public Task AddAsync(CeremonyState state)
        {
            Entries[state.IdHash] = state;
            return Task.CompletedTask;
        }

        public Task<CeremonyState?> TakeAsync(string idHash)
        {
            Entries.TryRemove(idHash, out var state);
            return Task.FromResult(state);
        }
    }
}