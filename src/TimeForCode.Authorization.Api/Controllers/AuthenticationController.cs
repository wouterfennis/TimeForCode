using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;
using System.Text.Json;
using TimeForCode.Authorization.Api.Mappers;
using TimeForCode.Authorization.Api.Models;
using TimeForCode.Authorization.Application.Interfaces;
using TimeForCode.Authorization.Commands;
using TimeForCode.Authorization.Values;

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

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthenticationController"/> class.
        /// </summary>
        /// <param name="sender">The sender.</param>
        public AuthenticationController(ISender sender)
        {
            _sender = sender;
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
        [ProducesResponseType(StatusCodes.Status302Found)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> LoginAsync(LoginRequestModel loginModel)
        {
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
        [ProducesResponseType(typeof(CallbackResponseModel), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CallbackAsync([FromQuery] CallbackRequestModel callbackModel)
        {
            var tokenResult = await _sender.Send(callbackModel.MapToCommand());

            return ProcessTokenResult(tokenResult);
        }

        private IActionResult ProcessTokenResult(Result<TokenResult> tokenResult)
        {
            if (tokenResult.IsFailure)
            {
                return BadRequest(ProblemDetailsMapper.BadRequest(tokenResult.ErrorMessage));
            }

            var result = tokenResult.Value!;

            SetTokenResponseCookies(result);

            // This is for the time being for debugging purposes.
            var response = new CallbackResponseModel
            {
                AccessToken = tokenResult.Value!.InternalAccessToken,
                RefreshToken = tokenResult.Value!.RefreshToken
            };

            return Ok(response);
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

            HttpContext.Response.Cookies.Append(Constants.CookieTokenKey, JsonSerializer.Serialize(result.InternalAccessToken), accessTokenCookieOptions);
            HttpContext.Response.Cookies.Append(Constants.CookieRefreshTokenKey, JsonSerializer.Serialize(result.RefreshToken), refreshTokenCookieOptions);
        }

        /// <summary>
        /// Logout endpoint.
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("logout")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> LogoutAsync()
        {
            var accessToken = GetAccessToken();
            var refreshToken = GetRefreshToken();

            var command = new LogoutCommand { 
                RefreshToken = refreshToken,
                AccessToken = accessToken 
            };

            await _sender.Send(command);

            DeleteTokenResponseCookies();

            return Ok();
        }

        private AccessToken? GetAccessToken()
        {
            if (HttpContext.Request.Cookies.TryGetValue(Constants.CookieTokenKey, out var accessToken))
            {
                return JsonSerializer.Deserialize<AccessToken>(accessToken);
            }

            return null;
        }

        private RefreshToken? GetRefreshToken()
        {
            if (HttpContext.Request.Cookies.TryGetValue(Constants.CookieRefreshTokenKey, out var refreshToken))
            {
                return JsonSerializer.Deserialize<RefreshToken>(refreshToken);
            }

            return null;
        }

        private void DeleteTokenResponseCookies()
        {
            HttpContext.Response.Cookies.Delete(Constants.CookieTokenKey);
            HttpContext.Response.Cookies.Delete(Constants.CookieRefreshTokenKey);
        }

        /// <summary>
        /// Refresh endpoint.
        /// </summary>
        /// <returns>New access token and refresh token</returns>
        [HttpPost]
        [Route("refresh")]
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

            return ProcessTokenResult(tokenResult);
        }
    }


}