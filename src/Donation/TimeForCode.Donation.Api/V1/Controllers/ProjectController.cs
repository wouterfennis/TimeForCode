using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.ComponentModel.DataAnnotations;
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
            var login = User.FindFirst("login")?.Value;
            var avatarUrl = User.FindFirst("avatar_url")?.Value;
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(login) || string.IsNullOrEmpty(avatarUrl))
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "User identity could not be determined",
                    Detail = "The token does not contain a valid user identifier, GitHub login and avatar.",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            var command = new RegisterProjectCommand
            {
                GithubRepositoryUrl = request.GithubRepositoryUrl,
                UserId = userId,
                GithubLogin = login,
                AvatarUrl = avatarUrl
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
        /// Searches the active projects (anonymous); other states are never returned.
        /// </summary>
        /// <remarks>Results are sorted by publication date, newest first, with the project id (descending) as tie-breaker.</remarks>
        /// <param name="pageNumber">The page number (1 to 10000, default 1).</param>
        /// <param name="pageSize">The number of items per page (1 to 100, default 20).</param>
        /// <param name="searchText">Optional text (at most 100 characters); matches the project name, full name or description (case-insensitive, literal substring).</param>
        /// <param name="language">Optional exact programming language, for example <c>C#</c>.</param>
        /// <param name="tag">Optional topic (case-insensitive); only projects with this topic are returned.</param>
        /// <returns>A paginated list of active projects matching all supplied criteria, with the total match count.</returns>
        [HttpGet(Name = nameof(GetProjects))]
        [ProducesResponseType(typeof(GetProjectsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetProjects(
            [FromQuery, Range(1, ApiConstraints.MaxPageNumber)] int pageNumber = 1,
            [FromQuery, Range(1, ApiConstraints.MaxPageSize)] int pageSize = 20,
            [FromQuery, MaxLength(ApiConstraints.MaxSearchCriterionLength)] string? searchText = null,
            [FromQuery, MaxLength(ApiConstraints.MaxSearchCriterionLength)] string? language = null,
            [FromQuery, MaxLength(ApiConstraints.MaxSearchCriterionLength)] string? tag = null)
        {
            var query = new GetProjectsQuery
            {
                SearchText = searchText,
                Language = language,
                Tag = tag,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
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
        public async Task<IActionResult> GetProjectById([ObjectId] string id)
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
        /// Returns the maintainer's own project in any lifecycle state, including the reviewer reason. Maintainer only.
        /// </summary>
        /// <param name="id">The project identifier.</param>
        /// <returns>The project details, its lifecycle state and the reviewer reason when changes were requested.</returns>
        [HttpGet("{id}/manage", Name = nameof(GetMaintainedProject))]
        [ProducesResponseType(typeof(GetMaintainedProjectResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [Authorize(Policy = "ApiUser")]
        public async Task<IActionResult> GetMaintainedProject([ObjectId] string id)
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

            var result = await _mediator.Send(new GetMaintainedProjectQuery { ProjectId = id, UserId = userId });

            if (result.IsSuccess)
            {
                return Ok(result.Value);
            }

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
        public Task<IActionResult> SubmitProjectForReview([ObjectId] string id)
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
        public async Task<IActionResult> ApproveProject([ObjectId] string id)
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
        public async Task<IActionResult> RequestProjectChanges([ObjectId] string id, RequestProjectChangesRequest request)
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
        public Task<IActionResult> ArchiveProject([ObjectId] string id)
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
        public Task<IActionResult> ReactivateProject([ObjectId] string id)
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