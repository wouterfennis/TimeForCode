using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.Net.Mime;
using System.Security.Cryptography;
using TimeForCode.Authorization.Application.Services;
using TimeForCode.Shared.Api.Authentication.Models;

namespace TimeForCode.Authorization.Api.Controllers
{
    /// <summary>
    /// Controller to expose JSON Web Key Set (JWKS) endpoint.
    /// </summary>
    [Produces(MediaTypeNames.Application.Json)]
    [Route(".well-known")]
    [ApiController]
    public class JwksController : ControllerBase
    {
        private readonly ILogger<JwksController> _logger;
        private readonly SigningKeySet _signingKeys;

        /// <summary>
        /// Initializes a new instance of the <see cref="JwksController"/> class.
        /// </summary>
        /// <param name="logger">Logger instance for logging.</param>
        /// <param name="signingKeys">The current and previous signing keys to publish.</param>
        public JwksController(ILogger<JwksController> logger, SigningKeySet signingKeys)
        {
            _logger = logger;
            _signingKeys = signingKeys;
        }

        /// <summary>
        /// Returns the JSON Web Key Set (JWKS) for the application.
        /// </summary>
        /// <returns>A JSON object containing the JWKS.</returns>
        [HttpGet("jwks.json")]
        [ProducesResponseType(typeof(JwksResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetJwksJson()
        {
            _logger.LogDebug("jwks is returned");

            var keys = _signingKeys.PublishedKeys.Select(ToJwk).ToList();

            var jwks = new JwksResponse { Keys = keys };
            return Ok(jwks);
        }

        private static JwkResponse ToJwk(RsaSecurityKey key)
        {
            var parameters = key.Rsa.ExportParameters(includePrivateParameters: false);
            return new JwkResponse
            {
                KeyType = nameof(RSA),
                KeyId = key.KeyId,
                Use = SigningKeySet.Use,
                Algorithm = SigningKeySet.Algorithm,
                Exponent = Base64UrlEncoder.Encode(parameters.Exponent),
                Modulus = Base64UrlEncoder.Encode(parameters.Modulus)
            };
        }
    }
}