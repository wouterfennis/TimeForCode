namespace TimeForCode.Authorization.Values
{
    /// <summary>
    /// An access token issued by an external identity provider.
    /// </summary>
    public class ExternalAccessToken
    {
        /// <summary>
        /// The token value as issued by the provider.
        /// </summary>
        public required string Token { get; init; }
    }
}