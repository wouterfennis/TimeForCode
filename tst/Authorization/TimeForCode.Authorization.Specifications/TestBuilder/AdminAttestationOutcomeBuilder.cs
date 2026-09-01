using TimeForCode.Authorization.Application.Interfaces.Admin;

namespace TimeForCode.Authorization.Specifications.TestBuilder
{
    internal static class AdminAttestationOutcomeBuilder
    {
        public static AdminAttestationOutcome BuildSucceeded()
        {
            return new AdminAttestationOutcome
            {
                Succeeded = true,
                CredentialId = [1, 2, 3],
                PublicKey = [4, 5, 6],
                SignCount = 0,
                Transports = ["internal"],
                IsUserVerified = true,
                IsBackupEligible = false,
                IsBackedUp = false
            };
        }

        public static AdminAttestationOutcome BuildFailed()
        {
            return new AdminAttestationOutcome { Succeeded = false, ErrorMessage = "Attestation failed." };
        }
    }
}
