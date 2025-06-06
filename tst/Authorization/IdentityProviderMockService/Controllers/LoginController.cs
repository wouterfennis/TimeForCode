using IdentityProviderMockService.Models;
using IdentityProviderMockService.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Mime;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Web;

namespace IdentityProviderMockService.Controllers
{
    [Produces(MediaTypeNames.Application.Json)]
    [Route("login")]
    [ApiController]
    public class LoginController : ControllerBase
    {
        private readonly ILogger<LoginController> _logger;
        private readonly IMemoryCache _memoryCache;
        private readonly RsaSecurityKey _rsaSecurityKey;
        private readonly AuthenticationOptions _authenticationOptions;

        public LoginController(ILogger<LoginController> logger,
            IOptions<AuthenticationOptions> authenticationOptions,
            RSA rsa,
            IMemoryCache memoryCache)
        {
            _logger = logger;
            _memoryCache = memoryCache;
            _authenticationOptions = authenticationOptions.Value;

            _rsaSecurityKey = new RsaSecurityKey(rsa);
        }

        [HttpGet("oauth/authorize")]
        [ProducesResponseType(StatusCodes.Status302Found)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public IActionResult GetAuthorize(AuthorizeRequest request)
        {
            _logger.LogDebug("authorize is returned");

            string code = Guid.NewGuid().ToString("N");
            _memoryCache.Set(code, request);

            var uriBuilder = new UriBuilder(request.RedirectUri);

            var query = HttpUtility.ParseQueryString(string.Empty);
            query["state"] = request.State;
            query["code"] = code;

            uriBuilder.Query = query.ToString();

            return Redirect(uriBuilder.ToString());
        }

        [HttpPost("oauth/access_token")]
        [ProducesResponseType(typeof(AccessTokenResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetAccessToken([FromBody] AccessTokenRequest accessTokenRequest)
        {
            if (accessTokenRequest.ClientId != _authenticationOptions.ExpectedClientId || accessTokenRequest.ClientSecret != _authenticationOptions.ExpectedClientSecret)
            {
                _logger.LogDebug("Token is not returned");
                var problemDetails = new ProblemDetails
                {
                    Detail = "ClientId and secret not known",
                    Status = 404
                };

                return NotFound(problemDetails);
            }

            var codeExists = _memoryCache.TryGetValue(accessTokenRequest.Code, out AuthorizeRequest? authorizeDetails);
            if (!codeExists)
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
            var claims = new Dictionary<string, object>
            {
                { "scope", authorizeDetails!.Scope }
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity([new Claim("sub", authorizeDetails!.ClientId)]),
                Expires = DateTime.UtcNow.AddMinutes(_authenticationOptions.TokenExpiresInMinutes),
                Issuer = _authenticationOptions.Issuer,
                Audience = _authenticationOptions.Audience,
                Claims = claims,
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