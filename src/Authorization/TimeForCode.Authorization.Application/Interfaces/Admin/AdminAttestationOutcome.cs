namespace TimeForCode.Authorization.Application.Interfaces.Admin
{
    /// <summary>
    /// Outcome of verifying a WebAuthn attestation (passkey registration ceremony).
    /// </summary>
    public class AdminAttestationOutcome
    {
        public required bool Succeeded { get; init; }
        public string? ErrorMessage { get; init; }
        public byte[]? CredentialId { get; init; }
        public byte[]? PublicKey { get; init; }
        public long SignCount { get; init; }
        public string[]? Transports { get; init; }
        public bool IsUserVerified { get; init; }
        public bool IsBackupEligible { get; init; }
        public bool IsBackedUp { get; init; }
    }
}
