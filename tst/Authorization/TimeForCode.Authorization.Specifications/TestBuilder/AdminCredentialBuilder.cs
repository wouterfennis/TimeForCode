using TimeForCode.Authorization.Domain.Entities;

namespace TimeForCode.Authorization.Specifications.TestBuilder
{
    internal static class AdminCredentialBuilder
    {
        public static AdminCredential Build()
        {
            return new AdminCredential
            {
                Id = AdminCredential.SingletonId,
                CredentialId = [1, 2, 3],
                PublicKey = [4, 5, 6],
                SignCount = 0,
                Transports = ["internal"],
                IsUserVerified = true,
                IsBackupEligible = false,
                IsBackedUp = false,
                CreatedAt = DateTimeOffset.UtcNow
            };
        }
    }
}
