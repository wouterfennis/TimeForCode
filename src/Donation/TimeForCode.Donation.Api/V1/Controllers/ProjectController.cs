using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TimeForCode.Donation.Api.V1.Models;

namespace TimeForCode.Donation.Api.V1.Controllers
{
    /// <summary>
    /// Controller for managing project-related operations.
    /// </summary>
    [ApiController]
    [Route("api/v1/[controller]")]
    public class ProjectController : ControllerBase
    {
        private readonly ILogger<ProjectController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectController"/> class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        public ProjectController(ILogger<ProjectController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Registers a new project.
        /// </summary>
        /// <param name="request">The project registration request containing the GitHub repository URL.</param>
        /// <returns>A response indicating the result of the registration process.</returns>
        [HttpPost(Name = nameof(RegisterProject))]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [Authorize(Policy = "ApiUser")]
        public IActionResult RegisterProject(RegisterProjectRequest request)
        {
            _logger.LogDebug("RegisterProject endpoint called.");
            return Ok("Project registration endpoint is not implemented yet.");
        }

        /// <summary>
        /// Test
        /// </summary>
        /// <returns>A response indicating the result of the registration process.</returns>
        [HttpGet("test", Name = nameof(Get))]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [Authorize(Policy = "ApiUser")]
        public string Get() {
            _logger.LogDebug("Get endpoint called.");
            return "Project registration endpoint is not implemented yet.";
        }
    }
}
