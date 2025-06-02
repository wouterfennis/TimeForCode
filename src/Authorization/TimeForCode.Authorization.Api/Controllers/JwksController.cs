using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.Net.Mime;
using System.Security.Cryptography;
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
        private readonly RSAParameters _rsaParameters;

        /// <summary>
        /// Initializes a new instance of the <see cref="JwksController"/> class.
        /// </summary>
        /// <param name="logger">Logger instance for logging.</param>
        /// <param name="rsa">RSA instance to export parameters.</param>
        public JwksController(ILogger<JwksController> logger, RSA rsa)
        {
            _logger = logger;
            _rsaParameters = rsa.ExportParameters(includePrivateParameters: false);
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