using TimeForCode.Authorization.Domain.Entities;

namespace TimeForCode.Authorization.Application.Interfaces.Admin
{
    /// <summary>
    /// Persistence for the single, permanently-claimed admin WebAuthn credential.
    /// </summary>
    public interface IAdminCredentialRepository
    {
        Task<AdminCredential?> GetAsync();

        /// <summary>
        /// Attempts to atomically persist the first admin credential. Returns <c>false</c> if a credential
        /// has already been claimed, including under a concurrent race.
        /// </summary>
        Task<bool> TryClaimAsync(AdminCredential credential);

        Task UpdateSignCountAsync(long signCount);
    }
}