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
    /// Controller for the maintainer: the platform user who owns projects.
    /// </summary>
    [ApiController]
    [Produces(MediaTypeNames.Application.Json)]
    [Route("api/v1/maintainer")]
    [EnableRateLimiting("api")]
    public class MaintainerController : ControllerBase
    {
        private readonly IMediator _mediator;

        /// <summary>
        /// Initializes a new instance of the <see cref="MaintainerController"/> class.
        /// </summary>
        /// <param name="mediator">The MediatR mediator.</param>
        public MaintainerController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Returns the maintainer profile of the caller. The profile is created when the caller publishes their first project.
        /// </summary>
        /// <returns>The profile; not found when the caller never published a project.</returns>
        /// <response code="200">The maintainer profile of the caller.</response>
        /// <response code="400">The token does not identify the user.</response>
        /// <response code="401">The access token is missing, invalid or expired.</response>
        /// <response code="404">The caller never published a project.</response>
        /// <response code="500">The request could not be processed because of an unexpected error.</response>
        [HttpGet("me", Name = nameof(GetMyMaintainerProfile))]
        [ProducesResponseType(typeof(MaintainerProfileResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [Authorize(Policy = "ApiUser")]
        public async Task<IActionResult> GetMyMaintainerProfile()
        {
            if (!TryGetUserId(out var userId))
            {
                return MissingIdentity();
            }

            var result = await _mediator.Send(new GetMaintainerProfileQuery { UserId = userId });
            if (result.IsFailure)
            {
                return ToProblem(result.FailureStatusCode, "Cannot get maintainer profile", result.ErrorMessage);
            }

            var profile = result.Value.Profile;
            return Ok(new MaintainerProfileResponse
            {
                Id = profile.Id,
                GithubLogin = profile.GithubLogin,
                AvatarUrl = profile.AvatarUrl
            });
        }

        /// <summary>
        /// Lists the projects published by the caller in any lifecycle state, oldest first, with the hours donated to each.
        /// Hours donated add up the hours committed by Active and Completed donations.
        /// </summary>
        /// <returns>The projects of the caller; an empty list when there are none.</returns>
        /// <response code="200">The projects of the caller with the hours donated to each, oldest first; an empty list when there are none.</response>
        /// <response code="400">The token does not identify the user.</response>
        /// <response code="401">The access token is missing, invalid or expired.</response>
        /// <response code="500">The request could not be processed because of an unexpected error.</response>
        [HttpGet("me/projects", Name = nameof(GetMyProjects))]
        [ProducesResponseType(typeof(GetMyProjectsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [Authorize(Policy = "ApiUser")]
        public async Task<IActionResult> GetMyProjects()
        {
            if (!TryGetUserId(out var userId))
            {
                return MissingIdentity();
            }

            var result = await _mediator.Send(new GetMyProjectsQuery { UserId = userId });

            if (result.IsFailure)
            {
                return ToProblem(result.FailureStatusCode, "Cannot get projects", result.ErrorMessage);
            }

            return Ok(new GetMyProjectsResponse
            {
                Projects = result.Value.Projects.Select(p => new MaintainedProjectResponse
                {
                    Project = ProjectResponse.From(p.Project),
                    HoursDonated = p.HoursDonated
                }).ToList()
            });
        }

        private bool TryGetUserId(out string userId)
        {
            userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
            return userId.Length > 0;
        }

        private BadRequestObjectResult MissingIdentity()
        {
            return BadRequest(new ProblemDetails
            {
                Title = "User identity could not be determined",
                Detail = "The token does not contain a valid user identifier.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        private ObjectResult ToProblem(HttpStatusCode? statusCode, string title, string detail)
        {
            var status = statusCode == HttpStatusCode.NotFound ? StatusCodes.Status404NotFound : StatusCodes.Status400BadRequest;

            return StatusCode(status, new ProblemDetails { Title = title, Detail = detail, Status = status });
        }
    }
}