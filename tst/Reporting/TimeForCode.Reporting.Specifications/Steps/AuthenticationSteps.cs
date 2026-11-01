using Microsoft.IdentityModel.Tokens;
using Reqnroll;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TimeForCode.Donation.Specifications.Mocking;
using TimeForCode.Reporting.Specifications.Mocking;

namespace TimeForCode.Reporting.Specifications.Steps
{
    [Binding]
    internal class AuthenticationSteps
    {
        private readonly BearerTokenHandler _bearerTokenHandler;
        private readonly LiveScenario _liveScenario;

        public AuthenticationSteps(BearerTokenHandler bearerTokenHandler, LiveScenario liveScenario)
        {
            _bearerTokenHandler = bearerTokenHandler;
            _liveScenario = liveScenario;
        }

        [Given("The user has an access token")]
        public async Task GivenTheUserHasAnAccessTokenAsync()
        {
            _bearerTokenHandler.Token = _liveScenario.IsLive
                ? await LiveAuthentication.GetUserTokenAsync()
                : GenerateTestToken(Constants.TestUserId);
        }

        [Given("The user has an administrator access token")]
        public async Task GivenTheUserHasAnAdministratorAccessTokenAsync()
        {
            _bearerTokenHandler.Token = _liveScenario.IsLive
                ? await LiveAuthentication.GetAdminTokenAsync()
                : GenerateTestToken("admin", "admin");
        }

        [Given("The user does not have an access token")]
        public void GivenTheUserDoesNotHaveAnAccessToken()
        {
            _bearerTokenHandler.Token = null;
        }

        private static string GenerateTestToken(string userId, string scope = "user")
        {
            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Constants.TestJwtSigningKey));
            var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim("scope", scope)
            };

            var token = new JwtSecurityToken(claims: claims, signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}