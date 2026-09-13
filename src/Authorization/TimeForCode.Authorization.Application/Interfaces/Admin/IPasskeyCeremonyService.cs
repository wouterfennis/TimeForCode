using TimeForCode.Authorization.Commands.Admin;

namespace TimeForCode.Authorization.Application.Interfaces.Admin
{
    /// <summary>
    /// Application-layer seam over the WebAuthn passkey ceremony. Exposes only our own DTOs — no ASP.NET
    /// Core Identity types (e.g. <c>IPasskeyHandler&lt;TUser&gt;</c>) leak past the Infrastructure implementation.
    /// If the underlying library is ever replaced, only the Infrastructure implementation and its DI
    /// registration change.
    /// </summary>
    public interface IPasskeyCeremonyService
    {
        Task<PasskeyCeremonyOptionsResult> CreateRegistrationOptionsAsync();

        Task<AdminAttestationOutcome> CompleteRegistrationAsync(string credentialJson, string ceremonyState);

        /// <summary>
        /// Builds request options whose <c>allowCredentials</c> list contains only the single registered
        /// admin credential id, so any other device is rejected by the browser/authenticator itself.
        /// </summary>
        Task<PasskeyCeremonyOptionsResult> CreateAuthenticationOptionsAsync();

        Task<AdminAssertionOutcome> CompleteAuthenticationAsync(string credentialJson, string ceremonyState);
    }
}