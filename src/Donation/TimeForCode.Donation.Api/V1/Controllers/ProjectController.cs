using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Mime;
using System.Security.Claims;
using TimeForCode.Donation.Api.V1.Models;
using TimeForCode.Donation.Commands;

namespace TimeForCode.Donation.Api.V1.Controllers
{
    /// <summary>
    /// Controller for managing project-related operations.
    /// </summary>
    [ApiController]
    [Produces(MediaTypeNames.Application.Json)]
    [Route("api/v1/[controller]")]
    public class ProjectController : ControllerBase
    {
        private readonly IMediator _mediator;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectController"/> class.
        /// </summary>
        /// <param name="mediator">The MediatR mediator.</param>
        public ProjectController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Publishes a GitHub repository as a project.
        /// </summary>
        /// <param name="request">The project registration request containing the GitHub repository URL.</param>
        /// <returns>A response indicating the result of the registration process.</returns>
        [HttpPost(Name = nameof(RegisterProject))]
        [ProducesResponseType(typeof(RegisterProjectResult), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [Authorize(Policy = "ApiUser")]
        public async Task<IActionResult> RegisterProject(RegisterProjectRequest request)
        {
            var userId = User.Claims.SingleOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "User identity could not be determined",
                    Detail = "The token does not contain a valid user identifier.",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            var command = new RegisterProjectCommand
            {
                GithubRepositoryUrl = request.GithubRepositoryUrl,
                UserId = userId
            };

            var result = await _mediator.Send(command);

            if (result.IsFailure)
            {
                if (result.FailureStatusCode == HttpStatusCode.Conflict)
                {
                    return Conflict(new ProblemDetails
                    {
                        Title = "Repository already published",
                        Detail = result.ErrorMessage,
                        Status = StatusCodes.Status409Conflict
                    });
                }

                return BadRequest(new ProblemDetails
                {
                    Title = "Cannot publish repository",
                    Detail = result.ErrorMessage,
                    Status = StatusCodes.Status400BadRequest
                });
            }

            return CreatedAtAction(nameof(GetProjectById), new { id = result.Value.ProjectId }, result.Value);
        }

        /// <summary>
        /// Returns a paginated list of all published projects.
        /// </summary>
        /// <param name="pageNumber">The page number (1-based, default 1).</param>
        /// <param name="pageSize">The number of items per page (default 20).</param>
        /// <returns>A paginated list of published projects.</returns>
        [HttpGet(Name = nameof(GetProjects))]
        [ProducesResponseType(typeof(GetProjectsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetProjects([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
        {
            if (pageNumber < 1)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid page number",
                    Detail = "Page number must be greater than or equal to 1.",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            if (pageSize < 1 || pageSize > 100)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid page size",
                    Detail = "Page size must be between 1 and 100.",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            var query = new GetProjectsQuery { PageNumber = pageNumber, PageSize = pageSize };
            var result = await _mediator.Send(query);

            if (result.IsFailure)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Could not retrieve projects",
                    Detail = result.ErrorMessage,
                    Status = StatusCodes.Status400BadRequest
                });
            }

            var response = new GetProjectsResponse
            {
                Projects = result.Value.Projects.Select(p => new ProjectSummaryResponse
                {
                    Id = p.Id,
                    Name = p.Name,
                    FullName = p.FullName,
                    Description = p.Description,
                    GithubUrl = p.GithubUrl,
                    Language = p.Language,
                    StargazersCount = p.StargazersCount,
                    OwnerLogin = p.OwnerLogin,
                    OwnerAvatarUrl = p.OwnerAvatarUrl
                }).ToList(),
                TotalCount = result.Value.TotalCount,
                PageNumber = result.Value.PageNumber,
                PageSize = result.Value.PageSize
            };

            return Ok(response);
        }

        /// <summary>
        /// Returns the full details of a published project.
        /// </summary>
        /// <param name="id">The project identifier.</param>
        /// <returns>Full project details.</returns>
        [HttpGet("{id}", Name = nameof(GetProjectById))]
        [ProducesResponseType(typeof(ProjectResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetProjectById(string id)
        {
            var query = new GetProjectByIdQuery { ProjectId = id };
            var result = await _mediator.Send(query);

            if (result.IsFailure)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Project not found",
                    Detail = result.ErrorMessage,
                    Status = StatusCodes.Status404NotFound
                });
            }

            var p = result.Value.Project;
            var response = new ProjectResponse
            {
                Id = p.Id,
                Name = p.Name,
                FullName = p.FullName,
                Description = p.Description,
                GithubUrl = p.GithubUrl,
                Language = p.Language,
                Topics = p.Topics,
                StargazersCount = p.StargazersCount,
                ForksCount = p.ForksCount,
                OpenIssuesCount = p.OpenIssuesCount,
                Homepage = p.Homepage,
                DefaultBranch = p.DefaultBranch,
                License = p.License,
                OwnerLogin = p.OwnerLogin,
                OwnerAvatarUrl = p.OwnerAvatarUrl,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,
                PushedAt = p.PushedAt,
                Status = p.Status
            };

            return Ok(response);
        }

        /// <summary>
        /// Unpublishes (archives) a previously published project.
        /// </summary>
        /// <param name="id">The project identifier.</param>
        /// <returns>No content on success.</returns>
        [HttpDelete("{id}", Name = nameof(UnpublishProject))]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [Authorize(Policy = "ApiUser")]
        public async Task<IActionResult> UnpublishProject(string id)
        {
            var userId = User.Claims.SingleOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "User identity could not be determined",
                    Detail = "The token does not contain a valid user identifier.",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            var command = new UnpublishProjectCommand { ProjectId = id, UserId = userId };
            var result = await _mediator.Send(command);

            if (result.IsFailure)
            {
                if (result.FailureStatusCode == HttpStatusCode.Forbidden)
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
                    {
                        Title = "Not authorized",
                        Detail = result.ErrorMessage,
                        Status = StatusCodes.Status403Forbidden
                    });
                }

                return NotFound(new ProblemDetails
                {
                    Title = "Project not found",
                    Detail = result.ErrorMessage,
                    Status = StatusCodes.Status404NotFound
                });
            }

            return NoContent();
        }
    }
}
