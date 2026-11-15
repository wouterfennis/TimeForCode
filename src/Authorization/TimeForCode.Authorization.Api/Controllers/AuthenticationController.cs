using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using System.Net.Mime;
using TimeForCode.Authorization.Api.Mappers;
using TimeForCode.Authorization.Api.Models;
using TimeForCode.Authorization.Application.Options;

namespace TimeForCode.Authorization.Api.Controllers
{
    /// <summary>
    /// Authentication endpoints. The API issues tokens; it sets and reads no cookies for this flow.
    /// </summary>
    [Route("api/v1/[controller]")]
    [Produces(MediaTypeNames.Application.Json)]
    [ApiController]
    public class AuthenticationController : ControllerBase
    {
        private readonly ISender _sender;
        private readonly IOptions<AuthenticationOptions> _authenticationOptions;
        private readonly ILogger<AuthenticationController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthenticationController"/> class.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="authenticationOptions">The authentication options.</param>
        /// <param name="logger">The logger.</param>
        public AuthenticationController(ISender sender, IOptions<AuthenticationOptions> authenticationOptions, ILogger<AuthenticationController> logger)
        {
            _sender = sender;
            _authenticationOptions = authenticationOptions;
            _logger = logger;
        }

        /// <summary>
        /// Login endpoint.
        /// </summary>
        /// <param name="loginModel">The model containing the requested external identity provider, the redirect URI and the PKCE code challenge.</param>
        /// <returns>
        /// The redirect URL towards an external identity provider.
        /// </returns>
        /// <response code="302">Redirect to the external identity provider's sign-in page.</response>
        /// <response code="400">The redirect uri is not on the list of allowed redirect uris, the code challenge is malformed or the request is not valid.</response>
        /// <response code="500">The login could not be started because of an unexpected server error.</response>
        [HttpGet]
        [Route("login")]
        [EnableRateLimiting("auth")]
        [ProducesResponseType(StatusCodes.Status302Found)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> LoginAsync(LoginRequestModel loginModel)
        {
            if (IsInvalidRedirectUri(loginModel.RedirectUri))
            {
                _logger.LogWarning("Redirect URI mismatch: '{RedirectUri}' is not in the allowed list.",
                    SanitizeForLog(loginModel.RedirectUri?.AbsoluteUri));
                return BadRequest(ProblemDetailsMapper.BadRequest("The supplied redirect uri is invalid"));
            }

            var redirectUrl = await _sender.Send(loginModel.MapToCommand());
            return Redirect(redirectUrl.AbsoluteUri);
        }

        /// <summary>
        /// Callback endpoint that is being called by the external identity provider. After the redirect initiated by <see cref="LoginAsync"/>.
        /// </summary>
        /// <param name="callbackModel">The model containing the authorization code from the external identity provider</param>
        /// <returns>
        /// A redirect to the redirect uri given at login, with a single-use <c>code</c> query parameter.
        /// </returns>
        /// <response code="302">Sign-in succeeded and the browser is redirected to the redirect uri given at login, with a single-use code to exchange for tokens.</response>
        /// <response code="400">The authorization code or state is missing, invalid or expired.</response>
        /// <response code="500">The sign-in could not be completed because of an unexpected server error.</response>
        [HttpGet]
        [Route("callback")]
        [EnableRateLimiting("auth")]
        [ProducesResponseType(StatusCodes.Status302Found)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CallbackAsync([FromQuery] CallbackRequestModel callbackModel)
        {
            var result = await _sender.Send(callbackModel.MapToCommand());

            if (result.IsFailure)
            {
                _logger.LogWarning("Authentication callback failed: {ErrorMessage}", SanitizeForLog(result.ErrorMessage));
                return BadRequest(ProblemDetailsMapper.BadRequest("Authentication failed."));
            }

            return Redirect(QueryHelpers.AddQueryString(result.Value.RedirectUri.ToString(), "code", result.Value.Code));
        }

        /// <summary>
        /// Exchanges the single-use code of a completed sign-in for tokens.
        /// </summary>
        /// <param name="model">The code and the PKCE verifier of the login that produced it.</param>
        /// <returns>The access token, the refresh token and whether the user is new.</returns>
        /// <response code="200">The access token and refresh token.</response>
        /// <response code="400">The code is unknown, already used or expired, or the verifier does not match the login.</response>
        /// <response code="500">The exchange failed because of an unexpected server error.</response>
        [HttpPost]
        [Route("token")]
        [EnableRateLimiting("auth")]
        [ProducesResponseType(typeof(TokenResponseModel), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ExchangeCodeAsync(ExchangeCodeRequestModel model)
        {
            var result = await _sender.Send(model.MapToCommand());

            if (result.IsFailure)
            {
                _logger.LogWarning("Code exchange denied: {ErrorMessage}", SanitizeForLog(result.ErrorMessage));
                return BadRequest(ProblemDetailsMapper.BadRequest("Code exchange failed."));
            }

            return Ok(result.Value.MapToResponse());
        }

        /// <summary>
        /// Refresh endpoint.
        /// </summary>
        /// <param name="model">The refresh token the client holds.</param>
        /// <returns>New access token and refresh token</returns>
        /// <response code="200">A new access token and refresh token. The refresh token that was sent is no longer valid.</response>
        /// <response code="400">The refresh token is unknown, expired or was already used.</response>
        /// <response code="500">The refresh failed because of an unexpected server error.</response>
        [HttpPost]
        [Route("refresh")]
        [EnableRateLimiting("auth")]
        [ProducesResponseType(typeof(TokenResponseModel), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> RefreshAsync(RefreshRequestModel model)
        {
            var result = await _sender.Send(model.MapToRefreshCommand());

            if (result.IsFailure)
            {
                _logger.LogWarning("Token refresh denied: {ErrorMessage}", SanitizeForLog(result.ErrorMessage));
                return BadRequest(ProblemDetailsMapper.BadRequest("Token refresh failed."));
            }

            return Ok(result.Value.MapToResponse());
        }

        /// <summary>
        /// Logout endpoint.
        /// </summary>
        /// <param name="model">The refresh token to revoke.</param>
        /// <returns></returns>
        /// <response code="204">The refresh token was revoked, if it existed.</response>
        /// <response code="500">The logout failed because of an unexpected server error.</response>
        [HttpPost]
        [Route("logout")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> LogoutAsync(RefreshRequestModel model)
        {
            await _sender.Send(model.MapToLogoutCommand());

            return NoContent();
        }

        private bool IsInvalidRedirectUri(Uri redirectUri)
        {
            return !_authenticationOptions.Value.ValidRedirectUris
                .Select(validRedirectUri => Uri.TryCreate(validRedirectUri, UriKind.Absolute, out Uri? parsedValidRedirectUri)
                    ? parsedValidRedirectUri
                    : null)
                .Any(validRedirectUri => validRedirectUri != null && RedirectUrisMatch(redirectUri, validRedirectUri));
        }

        private static string SanitizeForLog(string? value)
        {
            if (value is null) return "(null)";
            // Replace control characters (ASCII < 0x20) to prevent log injection / terminal manipulation.
            return new string(value.Select(c => c < 0x20 ? '_' : c).ToArray());
        }

        private static bool RedirectUrisMatch(Uri requestedRedirectUri, Uri validRedirectUri)
        {
            return string.Equals(requestedRedirectUri.Scheme, validRedirectUri.Scheme, StringComparison.OrdinalIgnoreCase)
                   && string.Equals(requestedRedirectUri.Host, validRedirectUri.Host, StringComparison.OrdinalIgnoreCase)
                   && requestedRedirectUri.Port == validRedirectUri.Port
                   && string.Equals(requestedRedirectUri.AbsolutePath.TrimEnd('/'), validRedirectUri.AbsolutePath.TrimEnd('/'), StringComparison.Ordinal)
                   && string.Equals(requestedRedirectUri.Query, validRedirectUri.Query, StringComparison.Ordinal)
                   && string.Equals(requestedRedirectUri.Fragment, validRedirectUri.Fragment, StringComparison.Ordinal);
        }
    }
}