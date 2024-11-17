using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;
using System.Security.Claims;
using TimeForCode.Authorization.Api.Models;
using TimeForCode.Authorization.Application.Interfaces;

namespace TimeForCode.Authorization.Api.Controllers
{
    /// <summary>
    /// User controller.
    /// </summary>
    [Produces(MediaTypeNames.Application.Json)]
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "ApiUser")]
    public class UserController : ControllerBase
    {
        private readonly IAccountInformationRepository _accountInformationRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserController"/> class.
        /// </summary>
        public UserController(IAccountInformationRepository accountInformationRepository)
        {
            _accountInformationRepository = accountInformationRepository;
        }

        /// <summary>
        /// Get user information.
        /// </summary>
        /// <returns></returns>
        [HttpGet("")]
        [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetUserAsync()
        {
            var userId = User.Claims.Single(x => x.Type == ClaimTypes.NameIdentifier)?.Value!;
            var userEntity = await _accountInformationRepository.GetByInternalIdAsync(userId);

            if (userEntity == null)
            {
                var problemDetails = new ProblemDetails
                {
                    Title = "User not found",
                    Detail = $"User with id {userId} not found"
                };

                return NotFound(problemDetails);
            }

            var user = new UserResponse
            {
                Id = userEntity.Id.ToString(),
                Name = userEntity.Name,
                Email = userEntity.Email,
                Login = userEntity.Login,
                NodeId = userEntity.NodeId,
                AvatarUrl = userEntity.AvatarUrl,
                Company = userEntity.Company
            };

            return Ok(user);
        }
    }
}