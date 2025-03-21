using IdentityProviderMockService.Models;
using IdentityProviderMockService.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Net.Mime;
using System.Security.Cryptography;

namespace IdentityProviderMockService.Controllers
{
    [Produces(MediaTypeNames.Application.Json)]
    [Route(".well-known")]
    [ApiController]
    public class JwksController : ControllerBase
    {
        private readonly ILogger<OpenIdConfigurationController> _logger;
        private readonly RSAParameters _rsaParameters;
        private readonly AuthenticationOptions _authenticationOptions;

        public JwksController(ILogger<OpenIdConfigurationController> logger,
            IOptions<AuthenticationOptions> authenticationOptions,
            RSA rsa)
        {
            _logger = logger;
            _authenticationOptions = authenticationOptions.Value;

            _rsaParameters = rsa.ExportParameters(includePrivateParameters: true);
        }

        [HttpGet("jwks.json")]
        [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetJwksJson()
        {
            _logger.LogDebug("jwks is returned");

            var jwk = new JwkResponse
            {
                KeyType = nameof(RSA),
                Exponent = Base64UrlEncoder.Encode(_rsaParameters.Exponent),
                Modulus = Base64UrlEncoder.Encode(_rsaParameters.Modulus)
            };

            var jwks = new JwksResponse { Keys = [jwk] };
            return Ok(jwks);
        }
    }
}