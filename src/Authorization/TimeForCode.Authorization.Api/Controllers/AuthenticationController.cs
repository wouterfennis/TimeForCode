using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using System.Net.Mime;
using System.Text.Json;
using TimeForCode.Authorization.Api.Mappers;
using TimeForCode.Authorization.Api.Models;
using TimeForCode.Authorization.Application.Options;
using TimeForCode.Authorization.Commands;
using TimeForCode.Authorization.Values;
using TimeForCode.Shared.Api.Authentication;

namespace TimeForCode.Authorization.Api.Controllers
{
    /// <summary>
    /// Authentication endpoints.
    /// </summary>
    [Route("api/[controller]")]
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
        /// <param name="loginModel">The model containing the requested external identity provider</param>
        /// <returns> 
        /// The redirect URL towards an external identity provider.
        /// </returns>
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
        /// The internal access token.
        /// </returns>
        [HttpGet]
        [Route("callback")]
        [EnableRateLimiting("auth")]
        [ProducesResponseType(typeof(CallbackResponseModel), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status302Found)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CallbackAsync([FromQuery] CallbackRequestModel callbackModel)
        {
            var tokenResult = await _sender.Send(callbackModel.MapToCommand());

            if (tokenResult.IsFailure)
            {
                _logger.LogWarning("Authentication callback failed: {ErrorMessage}", tokenResult.ErrorMessage);
                return BadRequest(ProblemDetailsMapper.BadRequest("Authentication failed."));
            }

            var response = ProcessTokenResult(tokenResult);

            if (tokenResult.Value.RedirectUri != null)
            {
                return Redirect(tokenResult.Value.RedirectUri.ToString());
            }

            return Ok(response);
        }

        /// <summary>
        /// Logout endpoint.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("logout")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> LogoutAsync()
        {
            var refreshToken = GetRefreshToken();

            var command = new LogoutCommand
            {
                RefreshToken = refreshToken,
            };

            await _sender.Send(command);

            return NoContent();
        }

        private bool IsInvalidRedirectUri(Uri redirectUri)
        {
            return !_authenticationOptions.Value.ValidRedirectUris
                .Any(validRedirectUri => string.Equals(
                    redirectUri.AbsoluteUri.TrimEnd('/'),
                    validRedirectUri.TrimEnd('/'),
                    StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Refresh endpoint.
        /// </summary>
        /// <returns>New access token and refresh token</returns>
        [HttpGet]
        [Route("refresh")]
        [EnableRateLimiting("auth")]
        [ProducesResponseType(typeof(CallbackResponseModel), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> RefreshAsync()
        {
            var refreshToken = GetRefreshToken();

            if (refreshToken is null)
            {
                return BadRequest(ProblemDetailsMapper.BadRequest("No refresh token found."));
            }

            var refreshCommand = new RefreshCommand
            {
                RefreshToken = refreshToken
            };

            var tokenResult = await _sender.Send(refreshCommand);

            if (tokenResult.IsFailure)
            {
                _logger.LogWarning("Token refresh denied: {ErrorMessage}", tokenResult.ErrorMessage);
                return BadRequest(ProblemDetailsMapper.BadRequest("Token refresh failed."));
            }

            var response = ProcessTokenResult(tokenResult);

            return Ok(response);
        }

        private CallbackResponseModel ProcessTokenResult(Result<TokenResult> tokenResult)
        {
            var result = tokenResult.Value!;

            SetTokenResponseCookies(result);

            var response = new CallbackResponseModel
            {
                AccessToken = tokenResult.Value!.InternalAccessToken,
                RefreshToken = tokenResult.Value!.RefreshToken,
                IsNewUser = tokenResult.Value!.IsNewUser
            };

            return response;
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
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = result.RefreshToken.ExpiresAfter
            };

            HttpContext.Response.Cookies.Append(CookieConstants.TokenKey, JsonSerializer.Serialize(result.InternalAccessToken), accessTokenCookieOptions);
            HttpContext.Response.Cookies.Append(CookieConstants.RefreshTokenKey, JsonSerializer.Serialize(result.RefreshToken), refreshTokenCookieOptions);

            if (result.IsNewUser)
            {
                var isNewUserCookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    MaxAge = TimeSpan.FromMinutes(5)
                };

                HttpContext.Response.Cookies.Append("IsNewUser", "true", isNewUserCookieOptions);
            }
        }

        private RefreshToken? GetRefreshToken()
        {
            if (HttpContext.Request.Cookies.TryGetValue(CookieConstants.RefreshTokenKey, out var refreshToken))
            {
                return JsonSerializer.Deserialize<RefreshToken>(refreshToken);
            }

            return null;
        }

        private static string SanitizeForLog(string? value)
        {
            if (value is null) return "(null)";
            return value.Replace('\n', '_').Replace('\r', '_');
        }
    }
}