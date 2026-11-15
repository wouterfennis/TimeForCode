using TimeForCode.Authorization.Domain.Entities;

namespace TimeForCode.Authorization.Application.Interfaces.Admin
{
    public interface ICeremonyStateRepository
    {
        Task AddAsync(CeremonyState state);

        /// <summary>
        /// Atomically returns and deletes the state, so a ceremony completes once.
        /// </summary>
        Task<CeremonyState?> TakeAsync(string idHash);
    }
}