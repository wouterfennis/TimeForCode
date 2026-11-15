namespace TimeForCode.Authorization.Specifications.TestBuilder
{
    /// <summary>
    /// How a scenario holds the access token of "the user": the spec client sends it as a bearer header. The platform itself
    /// has no cookie for it; this is only the name of the slot in the test cookie container.
    /// </summary>
    internal static class TokenWallet
    {
        public const string CookieName = "AccessToken";
    }
}