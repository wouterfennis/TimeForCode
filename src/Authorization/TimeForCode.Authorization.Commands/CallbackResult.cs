namespace TimeForCode.Authorization.Commands
{
    public class CallbackResult
    {
        /// <summary>
        /// Where the browser goes next: the redirect URI given at login.
        /// </summary>
        public required Uri RedirectUri { get; init; }

        /// <summary>
        /// The single-use code the client exchanges for tokens.
        /// </summary>
        public required string Code { get; init; }
    }
}