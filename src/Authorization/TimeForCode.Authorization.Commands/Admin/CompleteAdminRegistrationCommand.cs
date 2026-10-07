using MediatR;

namespace TimeForCode.Authorization.Commands.Admin
{
    /// <summary>
    /// Completes the one-time admin passkey registration ceremony. Only the first successful completion
    /// permanently claims the admin role; every subsequent attempt is rejected.
    /// </summary>
    public class CompleteAdminRegistrationCommand : IRequest<Result<TokenResult>>
    {
        public required string BootstrapSecret { get; init; }
        public required string CredentialJson { get; init; }
        public required string ProtectedCeremonyState { get; init; }
    }
}