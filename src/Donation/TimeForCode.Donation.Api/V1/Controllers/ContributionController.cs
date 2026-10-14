using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Net.Mime;
using System.Security.Claims;
using TimeForCode.Donation.Api.V1.Models;
using TimeForCode.Donation.Commands;

namespace TimeForCode.Donation.Api.V1.Controllers
{
    /// <summary>
    /// Controller for logging and listing the time contributions of a project.
    /// </summary>
    [ApiController]
    [Produces(MediaTypeNames.Application.Json)]
    [Route("api/v1/project/{id}/contribution")]
    [EnableRateLimiting("api")]
    public class ContributionController : ControllerBase
    {
        private const string LoginClaim = "login";
        private const string AvatarUrlClaim = "avatar_url";

        private readonly IMediator _mediator;

        /// <summary>
        /// Initializes a new instance of the <see cref="ContributionController"/> class.
        /// </summary>
        /// <param name="mediator">The MediatR mediator.</param>
        public ContributionController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Logs hours the caller spent on a published project.
        /// </summary>
        /// <param name="id">The project identifier.</param>
        /// <param name="request">The hours spent and an optional GitHub pull request or commit link.</param>
        /// <returns>The created contribution.</returns>
        [HttpPost(Name = nameof(AddContribution))]
        [ProducesResponseType(typeof(ContributionResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [Authorize(Policy = "ApiUser")]
        public async Task<IActionResult> AddContribution([ObjectId] string id, AddContributionRequest request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var login = User.FindFirst(LoginClaim)?.Value;
            var avatarUrl = User.FindFirst(AvatarUrlClaim)?.Value;
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(login) || string.IsNullOrEmpty(avatarUrl))
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "User identity could not be determined",
                    Detail = "The token does not contain a valid user identifier, GitHub login and avatar.",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            var command = new AddContributionCommand
            {
                ProjectId = id,
                UserId = userId,
                GithubLogin = login,
                AvatarUrl = avatarUrl,
                Hours = request.Hours,
                GithubReference = request.GithubReference
            };

            var result = await _mediator.Send(command);

            if (result.IsFailure)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Project not found",
                    Detail = result.ErrorMessage,
                    Status = StatusCodes.Status404NotFound
                });
            }

            var response = ToResponse(result.Value.Contribution);
            return CreatedAtAction(nameof(GetContributions), new { id }, response);
        }

        /// <summary>
        /// Returns the contributions logged on a published project (anonymous), earliest first.
        /// </summary>
        /// <param name="id">The project identifier.</param>
        /// <returns>The contributions of the project; an empty list when none were logged.</returns>
        [HttpGet(Name = nameof(GetContributions))]
        [ProducesResponseType(typeof(GetContributionsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetContributions([ObjectId] string id)
        {
            var result = await _mediator.Send(new GetContributionsQuery { ProjectId = id });

            if (result.IsFailure)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Project not found",
                    Detail = result.ErrorMessage,
                    Status = StatusCodes.Status404NotFound
                });
            }

            return Ok(new GetContributionsResponse
            {
                Contributions = result.Value.Contributions.Select(ToResponse).ToList()
            });
        }

        private static ContributionResponse ToResponse(ContributionDto dto)
        {
            return new ContributionResponse
            {
                Id = dto.Id,
                ProjectId = dto.ProjectId,
                ContributorId = dto.ContributorId,
                ContributorGitHubLogin = dto.ContributorGitHubLogin,
                ContributorAvatarUrl = dto.ContributorAvatarUrl,
                Hours = dto.Hours,
                GithubReference = dto.GithubReference,
                LoggedAt = dto.LoggedAt
            };
        }
    }
}