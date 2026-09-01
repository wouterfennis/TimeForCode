namespace TimeForCode.Authorization.Application.Interfaces.Admin
{
    /// <summary>
    /// Outcome of verifying a WebAuthn assertion (passkey authentication ceremony).
    /// </summary>
    public class AdminAssertionOutcome
    {
        public required bool Succeeded { get; init; }
        public string? ErrorMessage { get; init; }
        public long SignCount { get; init; }
    }
}
