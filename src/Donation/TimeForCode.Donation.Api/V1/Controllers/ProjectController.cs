using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
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
    [EnableRateLimiting("api")]
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
                    ForksCount = p.ForksCount,
                    OpenIssuesCount = p.OpenIssuesCount,
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
        /// Submits a draft project for review by an administrator. Maintainer only.
        /// </summary>
        /// <param name="id">The project identifier.</param>
        /// <returns>The new lifecycle state of the project.</returns>
        [HttpPost("{id}/submit", Name = nameof(SubmitProjectForReview))]
        [ProducesResponseType(typeof(ProjectLifecycleResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [Authorize(Policy = "ApiUser")]
        public Task<IActionResult> SubmitProjectForReview(string id)
        {
            return SendAsMaintainerAsync(userId => new SubmitProjectForReviewCommand { ProjectId = id, UserId = userId });
        }

        /// <summary>
        /// Approves a project that is pending approval, making it active. Administrator only.
        /// </summary>
        /// <param name="id">The project identifier.</param>
        /// <returns>The new lifecycle state of the project.</returns>
        [HttpPost("{id}/approve", Name = nameof(ApproveProject))]
        [ProducesResponseType(typeof(ProjectLifecycleResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [Authorize(Policy = "ApiAdmin")]
        public async Task<IActionResult> ApproveProject(string id)
        {
            var result = await _mediator.Send(new ApproveProjectCommand { ProjectId = id });
            return ToActionResult(result);
        }

        /// <summary>
        /// Sends a project that is pending approval back to draft with the reviewer reason. Administrator only.
        /// </summary>
        /// <param name="id">The project identifier.</param>
        /// <param name="request">The reason the changes are requested.</param>
        /// <returns>The new lifecycle state of the project including the reviewer reason.</returns>
        [HttpPost("{id}/request-changes", Name = nameof(RequestProjectChanges))]
        [ProducesResponseType(typeof(ProjectLifecycleResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [Authorize(Policy = "ApiAdmin")]
        public async Task<IActionResult> RequestProjectChanges(string id, RequestProjectChangesRequest request)
        {
            var result = await _mediator.Send(new RequestProjectChangesCommand { ProjectId = id, Reason = request.Reason });
            return ToActionResult(result);
        }

        /// <summary>
        /// Archives an active project. Maintainer only.
        /// </summary>
        /// <param name="id">The project identifier.</param>
        /// <returns>The new lifecycle state of the project.</returns>
        [HttpPost("{id}/archive", Name = nameof(ArchiveProject))]
        [ProducesResponseType(typeof(ProjectLifecycleResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [Authorize(Policy = "ApiUser")]
        public Task<IActionResult> ArchiveProject(string id)
        {
            return SendAsMaintainerAsync(userId => new ArchiveProjectCommand { ProjectId = id, UserId = userId });
        }

        /// <summary>
        /// Re-activates an archived project. Maintainer only.
        /// </summary>
        /// <param name="id">The project identifier.</param>
        /// <returns>The new lifecycle state of the project.</returns>
        [HttpPost("{id}/reactivate", Name = nameof(ReactivateProject))]
        [ProducesResponseType(typeof(ProjectLifecycleResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [Authorize(Policy = "ApiUser")]
        public Task<IActionResult> ReactivateProject(string id)
        {
            return SendAsMaintainerAsync(userId => new ReactivateProjectCommand { ProjectId = id, UserId = userId });
        }

        private async Task<IActionResult> SendAsMaintainerAsync(Func<string, IRequest<Result<ProjectLifecycleResult>>> createCommand)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "User identity could not be determined",
                    Detail = "The token does not contain a valid user identifier.",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            var result = await _mediator.Send(createCommand(userId));
            return ToActionResult(result);
        }

        private IActionResult ToActionResult(Result<ProjectLifecycleResult> result)
        {
            if (result.IsSuccess)
            {
                return Ok(result.Value);
            }

            return result.FailureStatusCode switch
            {
                HttpStatusCode.Forbidden => StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
                {
                    Title = "Not authorized",
                    Detail = result.ErrorMessage,
                    Status = StatusCodes.Status403Forbidden
                }),
                HttpStatusCode.Conflict => Conflict(new ProblemDetails
                {
                    Title = "Transition not allowed",
                    Detail = result.ErrorMessage,
                    Status = StatusCodes.Status409Conflict
                }),
                _ => NotFound(new ProblemDetails
                {
                    Title = "Project not found",
                    Detail = result.ErrorMessage,
                    Status = StatusCodes.Status404NotFound
                })
            };
        }
    }
}