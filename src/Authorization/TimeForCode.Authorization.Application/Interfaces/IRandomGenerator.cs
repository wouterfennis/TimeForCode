namespace TimeForCode.Authorization.Application.Interfaces
{
    public interface IRandomGenerator
    {
        public string GenerateRandomString();

        /// <summary>
        /// Generates a PKCE code verifier: 43 to 128 unreserved characters.
        /// </summary>
        public string GenerateCodeVerifier();
    }
}