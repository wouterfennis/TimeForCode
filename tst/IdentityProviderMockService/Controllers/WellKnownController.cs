using IdentityProviderMockService.Models;
using IdentityProviderMockService.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Web;

namespace IdentityProviderMockService.Controllers
{
    [ApiController]
    [Route(".well-known")]
    public class WellKnownController : ControllerBase
    {
        private readonly ILogger<WellKnownController> _logger;
        private readonly MemoryCache _memoryCache;
        private readonly RSAParameters _rsaParameters;
        private readonly RsaSecurityKey _rsaSecurityKey;
        private readonly AuthenticationOptions _authenticationOptions;

        public WellKnownController(ILogger<WellKnownController> logger, 
            IOptions<RsaKeyOptions> rsaKeyOptions,
            IOptions<AuthenticationOptions> authenticationOptions,
            MemoryCache memoryCache)
        {
            _logger = logger;
            _memoryCache = memoryCache;
            _authenticationOptions = authenticationOptions.Value;

            var rsa = RSA.Create();
            var rsaKey = rsaKeyOptions.Value;

            _rsaParameters = new RSAParameters
            {
                Modulus = Base64UrlEncoder.DecodeBytes(rsaKey.Modulus),
                Exponent = Base64UrlEncoder.DecodeBytes(rsaKey.Exponent),
                D = Base64UrlEncoder.DecodeBytes(rsaKey.D)
            };

            rsa.ImportParameters(_rsaParameters);
            _rsaSecurityKey = new RsaSecurityKey(rsa);
        }

        [HttpGet("openid-configuration")]
        public IActionResult GetOpenIdConfiguration()
        {
            _logger.LogDebug("OpenIdConfiguration is returned");

            var discoveryDocument = new DiscoveryDocumentResponse
            {
                Issuer = _authenticationOptions.Issuer,
                JwksUri = $"{_authenticationOptions.Issuer}/.well-known/jwks.json"
            };

            return Ok(discoveryDocument);
        }

        [HttpGet("jwks.json")]
        public IActionResult GetJwksJson()
        {
            _logger.LogDebug("jwks is returned");

            var jwk = new JwkResponse
            {
                KeyType = nameof(RSA),
                Exponent = Base64UrlEncoder.Encode(_rsaParameters.Exponent),
                Modulus = Base64UrlEncoder.Encode(_rsaParameters.Modulus)
            };

            var jwks = new JwksResponse{ Keys = [jwk] };
            return Ok(jwks);
        }

        [HttpGet("login/oauth/authorize")]
        public IActionResult GetAuthorize(string state, string redirectUri, string scope, string clientId)
        {
            _logger.LogDebug("authorize is returned");

            string code = Guid.NewGuid().ToString("N");
            _memoryCache.Set(code, new AuthorizeDetails
            {
                Scope = scope,
                ClientId = clientId,
            });

            var uriBuilder = new UriBuilder(redirectUri);

            var query = HttpUtility.ParseQueryString(string.Empty);
            query["state"] = state;
            query["code"] = code;

            uriBuilder.Query = query.ToString();

            return Redirect(uriBuilder.ToString());
        }

        [HttpPost("login/oauth/access_token")]
        public IActionResult GetAccessToken([FromBody] AccessTokenRequest accessTokenRequest)
        {
            if (accessTokenRequest.ClientId == _authenticationOptions.ExpectedClientId && accessTokenRequest.ClientSecret == _authenticationOptions.ExpectedClientSecret)
            {
                _logger.LogDebug("Token is not returned");
                var problemDetails = new ProblemDetails
                {
                    Detail = "ClientId and secret not known",
                    Status = 404
                };

                return NotFound(problemDetails);
            }

            var codeExists = _memoryCache.TryGetValue(accessTokenRequest.Code, out AuthorizeDetails? authorizeDetails);
            if (codeExists)
            {
                _logger.LogDebug("Authorization code is not found");
                var problemDetails = new ProblemDetails
                {
                    Detail = "Authorization code is not found",
                    Status = 404
                };

                return NotFound(problemDetails);
            }

            _logger.LogDebug("Token is returned");

            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity([new Claim("sub", authorizeDetails!.ClientId)]),
                Expires = DateTime.UtcNow.AddMinutes(_authenticationOptions.ExpiresInMinutes),
                Issuer = _authenticationOptions.Issuer,
                Audience = _authenticationOptions.Audience,
                SigningCredentials = new SigningCredentials(_rsaSecurityKey, SecurityAlgorithms.RsaSha256)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            return Ok(new AccessTokenResponse
            {
                TokenType = "Bearer",
                AccessToken = tokenString,
                Scope = authorizeDetails.Scope
            });
        }
    }
}
