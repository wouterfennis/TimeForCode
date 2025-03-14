using IdentityProviderMockService.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;
using System.Security.Claims;

namespace IdentityProviderMockService.Controllers
{
    [Produces(MediaTypeNames.Application.Json)]
    [Route("user")]
    [ApiController]
    [Authorize(Policy = "ApiUser")]
    public class UserController : ControllerBase
    {
        private readonly ILogger<UserController> _logger;

        public UserController(ILogger<UserController> logger)
        {
            _logger = logger;
        }

        [HttpGet("")]
        [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetUser()
        {
            _logger.LogDebug("user is returned");

            var user = new UserResponse
            {
                Id = 1,
                Name = User.Claims.Single(x => x.Type == ClaimTypes.NameIdentifier)?.Value!,
                Email = "something@something.org",
                Login = "johndoe",
                NodeId = "MDQ6VXNlcjE=",
                AvatarUrl = "https://avatars.githubusercontent.com/u/1?v=4",
                Company = "GitHub"
            };

            return Ok(user);
        }
    }
}