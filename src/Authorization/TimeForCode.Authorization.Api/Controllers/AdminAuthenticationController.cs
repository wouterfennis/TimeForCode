using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Net.Mime;
using TimeForCode.Authorization.Api.Mappers;
using TimeForCode.Authorization.Api.Models;
using TimeForCode.Authorization.Api.Models.Admin;
using TimeForCode.Authorization.Commands.Admin;

namespace TimeForCode.Authorization.Api.Controllers
{
    /// <summary>
    /// Admin WebAuthn passkey authentication endpoints. Deliberately a separate controller (not an
    /// extension of <see cref="AuthenticationController"/>) so admin routes, Swagger docs, and rate-limit
    /// policies are independently versionable/removable from the GitHub OAuth flow.
    /// The API sets and reads no cookie here: the state of a ceremony is kept server-side and identified by an opaque ceremony id,
    /// and the tokens are returned in the body to the client that holds the session (the front-end BFF).
    /// </summary>
    [Route("api/v1/admin-authentication")]
    [Produces(MediaTypeNames.Application.Json)]
    [ApiController]
    public class AdminAuthenticationController : ControllerBase
    {
        private readonly ISender _sender;
        private readonly ILogger<AdminAuthenticationController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="AdminAuthenticationController"/> class.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="logger">The logger.</param>
        public AdminAuthenticationController(ISender sender, ILogger<AdminAuthenticationController> logger)
        {
            _sender = sender;
            _logger = logger;
        }

        /// <summary>
        /// Begins the one-time admin passkey registration ceremony. Only available while no admin credential has been claimed yet.
        /// </summary>
        /// <param name="model">The bootstrap secret that proves the caller may claim the admin role.</param>
        /// <response code="200">The passkey creation options (JSON) for the browser and the ceremony id to send back when the ceremony completes.</response>
        /// <response code="400">The bootstrap secret is wrong or an admin credential has already been claimed.</response>
        [HttpPost]
        [Route("registration/options", Name = "AdminRegistrationOptions")]
        [EnableRateLimiting("admin-auth")]
        [ProducesResponseType(typeof(AdminPasskeyOptionsResponseModel), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RegistrationOptionsAsync(AdminRegistrationOptionsRequestModel model)
        {
            var command = new CreateAdminRegistrationOptionsCommand { BootstrapSecret = model.BootstrapSecret };
            var result = await _sender.Send(command);

            if (result.IsFailure)
            {
                _logger.LogWarning("Admin registration options request failed: {Error}", result.ErrorMessage);
                return BadRequest(ProblemDetailsMapper.BadRequest("The admin passkey request was refused."));
            }

            return Ok(new AdminPasskeyOptionsResponseModel { OptionsJson = result.Value.OptionsJson, CeremonyId = result.Value.CeremonyId });
        }

        /// <summary>
        /// Completes the one-time admin passkey registration ceremony. Only the first successful completion permanently claims the admin role.
        /// </summary>
        /// <param name="model">The bootstrap secret, the credential (JSON) created by the browser and the ceremony id of the options call.</param>
        /// <response code="200">The admin credential was claimed; the access token and refresh token are returned.</response>
        /// <response code="400">The ceremony is unknown, expired or already used, or the registration was rejected.</response>
        [HttpPost]
        [Route("registration", Name = "AdminCompleteRegistration")]
        [EnableRateLimiting("admin-auth")]
        [ProducesResponseType(typeof(TokenResponseModel), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CompleteRegistrationAsync(AdminCompleteRegistrationRequestModel model)
        {
            var command = new CompleteAdminRegistrationCommand
            {
                BootstrapSecret = model.BootstrapSecret,
                CredentialJson = model.CredentialJson,
                CeremonyId = model.CeremonyId
            };

            var result = await _sender.Send(command);

            if (result.IsFailure)
            {
                _logger.LogWarning("Admin registration completion failed: {Error}", result.ErrorMessage);
                return BadRequest(ProblemDetailsMapper.BadRequest("Admin passkey registration failed."));
            }

            return Ok(result.Value.MapToResponse());
        }

        /// <summary>
        /// Begins the admin passkey authentication ceremony. Requires an admin credential to already exist.
        /// </summary>
        /// <response code="200">The passkey request options (JSON) for the browser and the ceremony id to send back when the ceremony completes.</response>
        /// <response code="400">No admin credential has been registered yet.</response>
        [HttpPost]
        [Route("authentication/options", Name = "AdminAuthenticationOptions")]
        [EnableRateLimiting("admin-auth")]
        [ProducesResponseType(typeof(AdminPasskeyOptionsResponseModel), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AuthenticationOptionsAsync()
        {
            var result = await _sender.Send(new CreateAdminAuthenticationOptionsCommand());

            if (result.IsFailure)
            {
                _logger.LogWarning("Admin authentication options request failed: {Error}", result.ErrorMessage);
                return BadRequest(ProblemDetailsMapper.BadRequest("The admin passkey request was refused."));
            }

            return Ok(new AdminPasskeyOptionsResponseModel { OptionsJson = result.Value.OptionsJson, CeremonyId = result.Value.CeremonyId });
        }

        /// <summary>
        /// Completes the admin passkey authentication ceremony.
        /// </summary>
        /// <param name="model">The assertion credential (JSON) produced by the browser and the ceremony id of the options call.</param>
        /// <response code="200">The admin signed in; the access token and refresh token are returned.</response>
        /// <response code="400">The ceremony is unknown, expired or already used, or the assertion was rejected (including a signature counter that did not increase).</response>
        [HttpPost]
        [Route("authentication", Name = "AdminCompleteAuthentication")]
        [EnableRateLimiting("admin-auth")]
        [ProducesResponseType(typeof(TokenResponseModel), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CompleteAuthenticationAsync(AdminCompleteAuthenticationRequestModel model)
        {
            var command = new CompleteAdminAuthenticationCommand
            {
                CredentialJson = model.CredentialJson,
                CeremonyId = model.CeremonyId
            };

            var result = await _sender.Send(command);

            if (result.IsFailure)
            {
                _logger.LogWarning("Admin authentication failed: {Error}", result.ErrorMessage);
                return BadRequest(ProblemDetailsMapper.BadRequest("Admin authentication failed."));
            }

            return Ok(result.Value.MapToResponse());
        }
    }
}