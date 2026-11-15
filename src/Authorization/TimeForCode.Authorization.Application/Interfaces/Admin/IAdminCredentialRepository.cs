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

        /// <summary>
        /// Atomically stores the signature counter of an assertion, but only when it advances: higher than the stored one, or 0 on both sides
        /// (authenticators without a counter). False when it did not, which also covers two concurrent assertions racing on the same counter.
        /// </summary>
        Task<bool> TryAdvanceSignCountAsync(long presented);
    }
}