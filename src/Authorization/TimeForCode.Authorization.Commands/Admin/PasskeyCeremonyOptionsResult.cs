namespace TimeForCode.Authorization.Commands.Admin
{
    /// <summary>
    /// Options for a WebAuthn passkey ceremony (registration creation options or authentication request options),
    /// paired with a protected ceremony state that must be round-tripped by the caller (e.g. via an HttpOnly cookie).
    /// </summary>
    public class PasskeyCeremonyOptionsResult
    {
        public required string OptionsJson { get; init; }
        /// <summary>
        /// The opaque id of the ceremony the client sends back to complete it. The state itself stays on the server.
        /// </summary>
        public required string CeremonyId { get; init; }
    }
}