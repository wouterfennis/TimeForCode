using MediatR;
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
        [HttpPost]
        [Route("login")]
        [ProducesResponseType(StatusCodes.Status302Found)]
        public async Task<IActionResult> LoginAsync([FromBody] LoginModel loginModel)
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
        [HttpPost]
        [Route("callback")]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> CallbackAsync([FromQuery] CallbackModel callbackModel)
        {
            var callbackResult = await _sender.Send(callbackModel.MapToCommand());

            if(callbackResult.IsFailure)
            {
                return BadRequest(callbackResult.ErrorMessage);
            }

            return Ok(callbackResult.Data!.InternalAccessToken);
        }

        [HttpPost]
        [Route("logout")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult Logout()
        {
            return Ok();
        }

        [HttpPost]
        [Route("refresh")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult Refresh()
        {
            return Ok();
        }
    }


}
