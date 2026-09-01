using MediatR;

namespace TimeForCode.Authorization.Commands.Admin
{
    /// <summary>
    /// Requests the WebAuthn request options for the admin passkey authentication ceremony. The resulting
    /// options only allow the single registered admin credential to respond.
    /// </summary>
    public class CreateAdminAuthenticationOptionsCommand : IRequest<Result<PasskeyCeremonyOptionsResult>>
    {
    }
}
