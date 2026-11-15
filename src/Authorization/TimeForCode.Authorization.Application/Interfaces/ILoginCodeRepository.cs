using TimeForCode.Authorization.Domain.Entities;

namespace TimeForCode.Authorization.Application.Interfaces
{
    public interface ILoginCodeRepository
    {
        Task AddAsync(LoginCode loginCode);

        /// <summary>
        /// Atomically returns and deletes the code, so it can be used once.
        /// </summary>
        Task<LoginCode?> TakeAsync(string codeHash);
    }
}