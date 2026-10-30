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
    /// Controller for logging, listing, correcting and deleting the time contributions of a project.
    /// </summary>
    [ApiController]
    [Produces(MediaTypeNames.Application.Json)]
    [Route("api/v1/project/{id}/contribution")]
    [EnableRateLimiting("api")]
    public class ContributionController : ControllerBase
    {
        private const string LoginClaim = "login";
        private const string AvatarUrlClaim = "avatar_url";
        private const string AdminScope = "admin";

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

        /// <summary>
        /// Returns the contributions logged by the contributors affiliated with a donor organization, earliest first.
        /// Allowed for contributors affiliated with the organization and for administrators.
        /// </summary>
        /// <remarks>
        /// The list is not paged and holds at most 10000 contributions, earliest first; there is no indication when more exist, so totals
        /// built from it are incomplete for very large organizations. Contributions are attributed by the contributor's current
        /// affiliation, so a contributor who moves to another organization takes their history along. Deleted contributions are left out.
        /// </remarks>
        /// <param name="id">The donor organization identifier.</param>
        /// <returns>The contributions of the organization; an empty list when none were logged.</returns>
        [HttpGet("~/api/v1/donororganization/{id}/contribution", Name = nameof(GetDonorOrganizationContributions))]
        [ProducesResponseType(typeof(GetContributionsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [Authorize(Policy = "ApiUserOrAdmin")]
        public async Task<IActionResult> GetDonorOrganizationContributions([ObjectId] string id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return MissingIdentity();
            }

            var result = await _mediator.Send(new GetDonorOrganizationContributionsQuery
            {
                DonorOrganizationId = id,
                CallerUserId = userId,
                IsAdministrator = User.HasClaim("scope", AdminScope)
            });

            return result.IsFailure
                ? ToProblem(result.FailureStatusCode, "Cannot get contributions of donor organization", result.ErrorMessage)
                : Ok(new GetContributionsResponse
                {
                    Contributions = result.Value.Contributions.Select(ToResponse).ToList()
                });
        }

        /// <summary>
        /// Corrects the hours and GitHub reference of a contribution. Allowed for the contributor who logged it and for administrators.
        /// The previous values are kept as history.
        /// </summary>
        /// <param name="id">The project identifier.</param>
        /// <param name="contributionId">The contribution identifier.</param>
        /// <param name="request">The corrected hours and the optional GitHub reference (omitted removes it).</param>
        /// <returns>The corrected contribution.</returns>
        [HttpPut("{contributionId}", Name = nameof(CorrectContribution))]
        [ProducesResponseType(typeof(ContributionResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [Authorize(Policy = "ApiUserOrAdmin")]
        public async Task<IActionResult> CorrectContribution([ObjectId] string id, [ObjectId] string contributionId, CorrectContributionRequest request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return MissingIdentity();
            }

            var result = await _mediator.Send(new CorrectContributionCommand
            {
                ProjectId = id,
                ContributionId = contributionId,
                UserId = userId,
                IsAdministrator = User.HasClaim("scope", AdminScope),
                Hours = request.Hours,
                GithubReference = request.GithubReference
            });

            return result.IsFailure
                ? ToProblem(result.FailureStatusCode, "Cannot correct contribution", result.ErrorMessage)
                : Ok(ToResponse(result.Value.Contribution));
        }

        /// <summary>
        /// Deletes a contribution. Allowed for the contributor who logged it and for administrators.
        /// The contribution is only marked as deleted; it no longer appears in the list or counts towards the hours.
        /// </summary>
        /// <param name="id">The project identifier.</param>
        /// <param name="contributionId">The contribution identifier.</param>
        [HttpDelete("{contributionId}", Name = nameof(DeleteContribution))]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [Authorize(Policy = "ApiUserOrAdmin")]
        public async Task<IActionResult> DeleteContribution([ObjectId] string id, [ObjectId] string contributionId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return MissingIdentity();
            }

            var result = await _mediator.Send(new DeleteContributionCommand
            {
                ProjectId = id,
                ContributionId = contributionId,
                UserId = userId,
                IsAdministrator = User.HasClaim("scope", AdminScope)
            });

            return result.IsFailure
                ? ToProblem(result.FailureStatusCode, "Cannot delete contribution", result.ErrorMessage)
                : NoContent();
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
            var status = statusCode switch
            {
                HttpStatusCode.NotFound => StatusCodes.Status404NotFound,
                HttpStatusCode.Forbidden => StatusCodes.Status403Forbidden,
                HttpStatusCode.Conflict => StatusCodes.Status409Conflict,
                _ => StatusCodes.Status400BadRequest
            };

            return StatusCode(status, new ProblemDetails { Title = title, Detail = detail, Status = status });
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