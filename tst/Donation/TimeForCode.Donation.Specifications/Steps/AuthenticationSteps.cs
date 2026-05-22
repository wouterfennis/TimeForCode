using Microsoft.IdentityModel.Tokens;
using Reqnroll;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TimeForCode.Donation.Specifications.Mocking;

namespace TimeForCode.Donation.Specifications.Steps
{
    [Binding]
    internal class AuthenticationSteps
    {
        private readonly BearerTokenHandler _bearerTokenHandler;

        public AuthenticationSteps(BearerTokenHandler bearerTokenHandler)
        {
            _bearerTokenHandler = bearerTokenHandler;
        }

        [Given("The user has an access token")]
        public void GivenTheUserHasAnAccessToken()
        {
            _bearerTokenHandler.Token = GenerateTestToken(Constants.TestUserId);
        }

        [Given("The user does not have an access token")]
        public void GivenTheUserDoesNotHaveAnAccessToken()
        {
            _bearerTokenHandler.Token = null;
        }

        private static string GenerateTestToken(string userId)
        {
            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Constants.TestJwtSigningKey));
            var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim("scope", "user")
            };

            var token = new JwtSecurityToken(
                claims: claims,
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}