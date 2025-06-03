using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Net.Mime;
using TimeForCode.Authorization.Application.Options;
using TimeForCode.Shared.Api.Authentication.Models;

namespace TimeForCode.Authorization.Api.Controllers
{
    /// <summary>
    /// Controller to provide OpenID Connect configuration details.
    /// </summary>
    [Produces(MediaTypeNames.Application.Json)]
    [Route(".well-known")]
    [ApiController]
    public class OpenIdConfigurationController : ControllerBase
    {
        private readonly ILogger<OpenIdConfigurationController> _logger;
        private readonly AuthenticationOptions _authenticationOptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="OpenIdConfigurationController"/> class.
        /// </summary>
        /// <param name="logger">Logger instance for logging.</param>
        /// <param name="authenticationOptions">Authentication options configuration.</param>
        public OpenIdConfigurationController(ILogger<OpenIdConfigurationController> logger,
            IOptions<AuthenticationOptions> authenticationOptions)
        {
            _logger = logger;
            _authenticationOptions = authenticationOptions.Value;
        }

        /// <summary>
        /// Retrieves the OpenID Connect configuration document.
        /// </summary>
        /// <returns>A discovery document containing OpenID Connect configuration details.</returns>
        [HttpGet("openid-configuration")]
        [ProducesResponseType(typeof(DiscoveryDocumentResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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
    }
}