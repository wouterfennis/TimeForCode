using MediatR;

namespace TimeForCode.Authorization.Commands.Admin
{
    /// <summary>
    /// Completes the admin passkey authentication ceremony.
    /// </summary>
    public class CompleteAdminAuthenticationCommand : IRequest<Result<TokenResult>>
    {
        public required string CredentialJson { get; init; }
        public required string ProtectedCeremonyState { get; init; }
    }
}
