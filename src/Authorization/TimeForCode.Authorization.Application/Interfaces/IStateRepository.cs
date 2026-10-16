using TimeForCode.Authorization.Domain.Entities;

namespace TimeForCode.Authorization.Application.Interfaces
{
    public interface IStateRepository
    {
        Task AddAsync(StateEntry stateEntry);

        /// <summary>
        /// Atomically removes and returns the entry, so a state can be consumed only once. The caller must still check expiry.
        /// </summary>
        Task<StateEntry?> TakeAsync(string key);
    }
}