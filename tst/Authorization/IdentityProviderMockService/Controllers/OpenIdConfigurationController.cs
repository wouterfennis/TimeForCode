using IdentityProviderMockService.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Net.Mime;
using TimeForCode.Shared.Api.Authentication.Models;

namespace IdentityProviderMockService.Controllers
{
    [Produces(MediaTypeNames.Application.Json)]
    [Route(".well-known")]
    [ApiController]
    public class OpenIdConfigurationController : ControllerBase
    {
        private readonly ILogger<OpenIdConfigurationController> _logger;
        private readonly AuthenticationOptions _authenticationOptions;

        public OpenIdConfigurationController(ILogger<OpenIdConfigurationController> logger,
            IOptions<AuthenticationOptions> authenticationOptions)
        {
            _logger = logger;
            _authenticationOptions = authenticationOptions.Value;
        }

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