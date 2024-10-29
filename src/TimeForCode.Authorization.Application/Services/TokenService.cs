using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using TimeForCode.Authorization.Application.Interfaces;
using TimeForCode.Authorization.Application.Options;
using TimeForCode.Authorization.Commands;

namespace TimeForCode.Authorization.Application.Services
{
    public class TokenService : ITokenService
    {
        private readonly RsaSecurityKey _rsaSecurityKey;
        private readonly IIdentityProviderServiceFactory _identityProviderServiceFactory;
        private readonly TimeProvider _timeProvider;
        private readonly AuthenticationOptions _authenticationOptions;

        public TokenService(RSA rsa,
            IOptions<AuthenticationOptions> authenticationOptions,
            IIdentityProviderServiceFactory identityProviderServiceFactory,
            TimeProvider timeProvider)
        {
            _rsaSecurityKey = new RsaSecurityKey(rsa);
            _authenticationOptions = authenticationOptions.Value;
            _identityProviderServiceFactory = identityProviderServiceFactory;
            _timeProvider = timeProvider;
        }

        public async Task<Result<string>> GetAccessTokenFromExternalProvider(string state, string code)
        {
            var result = _identityProviderServiceFactory.GetIdentityProviderServiceFromState(state);
            if (result.IsFailure)
            {
                return Result<string>.Failure(result.ErrorMessage);
            }
            var identityProviderService = result.Value;

            var externalAccessTokenResult = await identityProviderService.GetAccessTokenAsync(code);
            if (externalAccessTokenResult.IsFailure)
            {
                return Result<string>.Failure(externalAccessTokenResult.ErrorMessage);
            }

            return Result<string>.Success(externalAccessTokenResult.Value.AccessToken);
        }

        public string GenerateInternalToken(string userId)
        {
            var claims = new Dictionary<string, object>
            {
                { "scope", "user" }
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity([new Claim("sub", userId)]),
                Expires = DateTime.UtcNow.AddMinutes(_authenticationOptions.ExpiresInMinutes),
                Issuer = _authenticationOptions.Issuer,
                Audience = _authenticationOptions.Audience,
                Claims = claims,
                SigningCredentials = new SigningCredentials(_rsaSecurityKey, SecurityAlgorithms.RsaSha256)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            return tokenString;
        }
    }
}
