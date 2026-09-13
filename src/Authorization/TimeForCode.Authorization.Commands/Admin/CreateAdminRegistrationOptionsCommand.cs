using MediatR;

namespace TimeForCode.Authorization.Commands.Admin
{
    /// <summary>
    /// Requests the WebAuthn creation options for the one-time admin passkey registration ceremony.
    /// </summary>
    public class CreateAdminRegistrationOptionsCommand : IRequest<Result<PasskeyCeremonyOptionsResult>>
    {
        public required string BootstrapSecret { get; init; }
    }
}