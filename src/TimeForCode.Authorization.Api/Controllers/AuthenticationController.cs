﻿using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;
using TimeForCode.Authorization.Api.Mappers;
using TimeForCode.Authorization.Api.Models;

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
        /// <param name="sender"> The sender. </param>
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
            var callbackResult = await _sender.Send(callbackModel.MapToCommand());

            if (callbackResult.IsFailure)
            {
                return BadRequest(ProblemDetailsMapper.BadRequest(callbackResult.ErrorMessage));
            }

            var accessTokenCookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddHours(1)
            };

            var refreshTokenCookieOptions = new CookieOptions(accessTokenCookieOptions);
            refreshTokenCookieOptions.Expires = DateTime.UtcNow.AddDays(7);
            HttpContext.Response.Cookies.Append("AccessToken", callbackResult.Value!.InternalAccessToken.Token, accessTokenCookieOptions);
            HttpContext.Response.Cookies.Append("RefreshToken", callbackResult.Value!.RefreshToken.Token, refreshTokenCookieOptions);

            // This is for the time being for debugging purposes.
            var response = new CallbackResponseModel
            {
                AccessToken = callbackResult.Value!.InternalAccessToken,
                RefreshToken = callbackResult.Value!.RefreshToken
            };

            return Ok(response);
        }

        /// <summary>
        /// Logout endpoint.
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("logout")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult Logout()
        {
            return Ok();
        }

        /// <summary>
        /// Refresh endpoint.
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("refresh")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult Refresh()
        {
            return Ok();
        }
    }


}