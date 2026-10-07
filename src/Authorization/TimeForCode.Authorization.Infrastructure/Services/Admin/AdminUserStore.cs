using Microsoft.AspNetCore.Identity;
using TimeForCode.Authorization.Application.Interfaces.Admin;
using TimeForCode.Authorization.Domain.Entities;

namespace TimeForCode.Authorization.Infrastructure.Services.Admin
{
    /// <summary>
    /// Backs the single conceptual admin user with the MongoDB-persisted <see cref="AdminCredential"/>.
    /// Only the members actually exercised by the passkey ceremony are meaningfully implemented; the rest
    /// are no-ops since no real user store exists behind this.
    /// </summary>
    internal class AdminUserStore : IUserStore<AdminPasskeyUser>, IUserPasskeyStore<AdminPasskeyUser>
    {
        private readonly IAdminCredentialRepository _adminCredentialRepository;

        public AdminUserStore(IAdminCredentialRepository adminCredentialRepository)
        {
            _adminCredentialRepository = adminCredentialRepository;
        }

        public Task<string> GetUserIdAsync(AdminPasskeyUser user, CancellationToken cancellationToken) => Task.FromResult(user.Id);

        public Task<string?> GetUserNameAsync(AdminPasskeyUser user, CancellationToken cancellationToken) => Task.FromResult<string?>(user.Id);

        public Task SetUserNameAsync(AdminPasskeyUser user, string? userName, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<string?> GetNormalizedUserNameAsync(AdminPasskeyUser user, CancellationToken cancellationToken) => Task.FromResult<string?>(user.Id);

        public Task SetNormalizedUserNameAsync(AdminPasskeyUser user, string? normalizedName, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<IdentityResult> CreateAsync(AdminPasskeyUser user, CancellationToken cancellationToken) => Task.FromResult(IdentityResult.Success);

        public Task<IdentityResult> UpdateAsync(AdminPasskeyUser user, CancellationToken cancellationToken) => Task.FromResult(IdentityResult.Success);

        public Task<IdentityResult> DeleteAsync(AdminPasskeyUser user, CancellationToken cancellationToken) => Task.FromResult(IdentityResult.Success);

        public Task<AdminPasskeyUser?> FindByIdAsync(string userId, CancellationToken cancellationToken)
        {
            return Task.FromResult(userId == AdminPasskeyUser.AdminUserId ? new AdminPasskeyUser() : null);
        }

        public Task<AdminPasskeyUser?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
        {
            return Task.FromResult(normalizedUserName == AdminPasskeyUser.AdminUserId ? new AdminPasskeyUser() : null);
        }

        public async Task AddOrUpdatePasskeyAsync(AdminPasskeyUser user, UserPasskeyInfo passkey, CancellationToken cancellationToken)
        {
            await _adminCredentialRepository.UpdateSignCountAsync(passkey.SignCount);
        }

        public async Task<IList<UserPasskeyInfo>> GetPasskeysAsync(AdminPasskeyUser user, CancellationToken cancellationToken)
        {
            var credential = await _adminCredentialRepository.GetAsync();
            return credential == null ? [] : [ToPasskeyInfo(credential)];
        }

        public async Task<AdminPasskeyUser?> FindByPasskeyIdAsync(byte[] credentialId, CancellationToken cancellationToken)
        {
            var credential = await _adminCredentialRepository.GetAsync();
            return credential != null && credential.CredentialId.AsSpan().SequenceEqual(credentialId) ? new AdminPasskeyUser() : null;
        }

        public async Task<UserPasskeyInfo?> FindPasskeyAsync(AdminPasskeyUser user, byte[] credentialId, CancellationToken cancellationToken)
        {
            var credential = await _adminCredentialRepository.GetAsync();
            return credential != null && credential.CredentialId.AsSpan().SequenceEqual(credentialId) ? ToPasskeyInfo(credential) : null;
        }

        public Task RemovePasskeyAsync(AdminPasskeyUser user, byte[] credentialId, CancellationToken cancellationToken)
        {
            // Reset flows (lost/broken device) are explicitly out of scope for this feature.
            throw new NotSupportedException("Removing the admin passkey is not supported.");
        }

        public void Dispose()
        {
        }

        private static UserPasskeyInfo ToPasskeyInfo(AdminCredential credential)
        {
            return new UserPasskeyInfo(
                credential.CredentialId,
                credential.PublicKey,
                credential.CreatedAt,
                (uint)credential.SignCount,
                credential.Transports,
                credential.IsUserVerified,
                credential.IsBackupEligible,
                credential.IsBackedUp,
                attestationObject: [],
                clientDataJson: []);
        }
    }
}