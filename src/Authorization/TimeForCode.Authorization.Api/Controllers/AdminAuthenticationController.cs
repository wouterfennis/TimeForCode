using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Net.Mime;
using System.Text.Json;
using TimeForCode.Authorization.Api.Mappers;
using TimeForCode.Authorization.Api.Models;
using TimeForCode.Authorization.Api.Models.Admin;
using TimeForCode.Authorization.Commands;
using TimeForCode.Authorization.Commands.Admin;
using TimeForCode.Shared.Api.Authentication;

namespace TimeForCode.Authorization.Api.Controllers
{
    /// <summary>
    /// Admin WebAuthn passkey authentication endpoints. Deliberately a separate controller (not an
    /// extension of <see cref="AuthenticationController"/>) so admin routes, Swagger docs, and rate-limit
    /// policies are independently versionable/removable from the GitHub OAuth flow.
    /// </summary>
    [Route("api/v1/admin-authentication")]
    [Produces(MediaTypeNames.Application.Json)]
    [ApiController]
    public class AdminAuthenticationController : ControllerBase
    {
        /// <summary>
        /// Cookie key for the encrypted attestation state, round-tripped across the registration ceremony.
        /// Public (not private) so the test project can seed it directly — Secure cookies set by the
        /// response aren't reliably round-tripped by <see cref="System.Net.CookieContainer"/> over the
        /// plain-HTTP test server.
        /// </summary>
        public const string AttestationStateCookieKey = "AdminAttestationState";

        /// <summary>
        /// Cookie key for the encrypted assertion state, round-tripped across the authentication ceremony.
        /// See <see cref="AttestationStateCookieKey"/> for why this is public.
        /// </summary>
        public const string AssertionStateCookieKey = "AdminAssertionState";

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
        /// Begins the one-time admin passkey registration ceremony. Only available while no admin
        /// credential has been claimed yet.
        /// </summary>
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
                return BadRequest(ProblemDetailsMapper.BadRequest(result.ErrorMessage));
            }

            SetCeremonyStateCookie(AttestationStateCookieKey, result.Value.ProtectedCeremonyState);
            return Ok(new AdminPasskeyOptionsResponseModel { OptionsJson = result.Value.OptionsJson });
        }

        /// <summary>
        /// Completes the one-time admin passkey registration ceremony. Only the first successful
        /// completion permanently claims the admin role.
        /// </summary>
        [HttpPost]
        [Route("registration", Name = "AdminCompleteRegistration")]
        [EnableRateLimiting("admin-auth")]
        [ProducesResponseType(typeof(CallbackResponseModel), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CompleteRegistrationAsync(AdminCompleteRegistrationRequestModel model)
        {
            var ceremonyState = GetCeremonyStateCookie(AttestationStateCookieKey);
            if (ceremonyState == null)
            {
                return BadRequest(ProblemDetailsMapper.BadRequest("No admin registration ceremony in progress."));
            }

            var command = new CompleteAdminRegistrationCommand
            {
                BootstrapSecret = model.BootstrapSecret,
                CredentialJson = model.CredentialJson,
                ProtectedCeremonyState = ceremonyState
            };

            var result = await _sender.Send(command);
            Response.Cookies.Delete(AttestationStateCookieKey);

            if (result.IsFailure)
            {
                _logger.LogWarning("Admin registration completion failed: {Error}", result.ErrorMessage);
                return BadRequest(ProblemDetailsMapper.BadRequest("Admin passkey registration failed."));
            }

            return Ok(ProcessTokenResult(result.Value));
        }

        /// <summary>
        /// Begins the admin passkey authentication ceremony. Requires an admin credential to already exist.
        /// </summary>
        [HttpGet]
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
                return BadRequest(ProblemDetailsMapper.BadRequest(result.ErrorMessage));
            }

            SetCeremonyStateCookie(AssertionStateCookieKey, result.Value.ProtectedCeremonyState);
            return Ok(new AdminPasskeyOptionsResponseModel { OptionsJson = result.Value.OptionsJson });
        }

        /// <summary>
        /// Completes the admin passkey authentication ceremony.
        /// </summary>
        [HttpPost]
        [Route("authentication", Name = "AdminCompleteAuthentication")]
        [EnableRateLimiting("admin-auth")]
        [ProducesResponseType(typeof(CallbackResponseModel), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CompleteAuthenticationAsync(AdminCompleteAuthenticationRequestModel model)
        {
            var ceremonyState = GetCeremonyStateCookie(AssertionStateCookieKey);
            if (ceremonyState == null)
            {
                return BadRequest(ProblemDetailsMapper.BadRequest("No admin authentication ceremony in progress."));
            }

            var command = new CompleteAdminAuthenticationCommand
            {
                CredentialJson = model.CredentialJson,
                ProtectedCeremonyState = ceremonyState
            };

            var result = await _sender.Send(command);
            Response.Cookies.Delete(AssertionStateCookieKey);

            if (result.IsFailure)
            {
                _logger.LogWarning("Admin authentication failed: {Error}", result.ErrorMessage);
                return BadRequest(ProblemDetailsMapper.BadRequest("Admin authentication failed."));
            }

            return Ok(ProcessTokenResult(result.Value));
        }

        private CallbackResponseModel ProcessTokenResult(TokenResult result)
        {
            SetTokenResponseCookies(result);

            return new CallbackResponseModel
            {
                AccessToken = result.InternalAccessToken,
                RefreshToken = result.RefreshToken,
                IsNewUser = result.IsNewUser
            };
        }

        private void SetTokenResponseCookies(TokenResult result)
        {
            var accessTokenCookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = result.InternalAccessToken.ExpiresAfter
            };

            var refreshTokenCookieOptions = new CookieOptions(accessTokenCookieOptions)
            {
                Expires = result.RefreshToken.ExpiresAfter
            };

            Response.Cookies.Append(CookieConstants.TokenKey, JsonSerializer.Serialize(result.InternalAccessToken), accessTokenCookieOptions);
            Response.Cookies.Append(CookieConstants.RefreshTokenKey, JsonSerializer.Serialize(result.RefreshToken), refreshTokenCookieOptions);
        }

        private void SetCeremonyStateCookie(string key, string protectedState)
        {
            Response.Cookies.Append(key, protectedState, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                MaxAge = TimeSpan.FromMinutes(5)
            });
        }

        private string? GetCeremonyStateCookie(string key)
        {
            return Request.Cookies.TryGetValue(key, out var value) ? value : null;
        }
    }
}
