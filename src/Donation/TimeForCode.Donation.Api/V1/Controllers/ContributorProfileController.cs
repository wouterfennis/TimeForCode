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
    /// Controller for the contributor profile: role, skills, hours available and donor organization affiliation.
    /// </summary>
    [ApiController]
    [Produces(MediaTypeNames.Application.Json)]
    [Route("api/v1")]
    [EnableRateLimiting("api")]
    public class ContributorProfileController : ControllerBase
    {
        private const string LoginClaim = "login";
        private const string AvatarUrlClaim = "avatar_url";
        private const string AdminScope = "admin";

        private readonly IMediator _mediator;

        /// <summary>
        /// Initializes a new instance of the <see cref="ContributorProfileController"/> class.
        /// </summary>
        /// <param name="mediator">The MediatR mediator.</param>
        public ContributorProfileController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Returns the contributor profile of the caller.
        /// </summary>
        /// <returns>The profile; not found when the caller never became a contributor.</returns>
        [HttpGet("contributor/me", Name = nameof(GetMyContributorProfile))]
        [ProducesResponseType(typeof(ContributorProfileResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [Authorize(Policy = "ApiUser")]
        public async Task<IActionResult> GetMyContributorProfile()
        {
            if (!TryGetUserId(out var userId))
            {
                return MissingIdentity();
            }

            var result = await _mediator.Send(new GetContributorProfileQuery { CallerUserId = userId });

            return result.IsFailure
                ? ToProblem(result.FailureStatusCode, "Cannot get contributor profile", result.ErrorMessage)
                : Ok(ToResponse(result.Value.Profile));
        }

        /// <summary>
        /// Replaces the contributor profile of the caller, registering the caller as a contributor when needed.
        /// Fields that are left out are cleared, so send the whole profile.
        /// </summary>
        /// <param name="request">The role, skills, hours available and optional donor organization.</param>
        /// <returns>The updated profile.</returns>
        [HttpPut("contributor/me", Name = nameof(UpdateMyContributorProfile))]
        [ProducesResponseType(typeof(ContributorProfileResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [Authorize(Policy = "ApiUser")]
        public async Task<IActionResult> UpdateMyContributorProfile(UpdateContributorProfileRequest request)
        {
            var login = User.FindFirst(LoginClaim)?.Value;
            var avatarUrl = User.FindFirst(AvatarUrlClaim)?.Value;
            if (!TryGetUserId(out var userId) || string.IsNullOrEmpty(login) || string.IsNullOrEmpty(avatarUrl))
            {
                return MissingIdentity();
            }

            var result = await _mediator.Send(ToCommand(request, userId, login, avatarUrl, contributorId: null, isAdministrator: false));

            return result.IsFailure
                ? ToProblem(result.FailureStatusCode, "Cannot update contributor profile", result.ErrorMessage)
                : Ok(ToResponse(result.Value.Profile));
        }

        /// <summary>
        /// Returns a contributor profile. Allowed for the contributor themselves and for administrators.
        /// </summary>
        /// <param name="id">The contributor identifier.</param>
        /// <returns>The profile.</returns>
        [HttpGet("contributor/{id}", Name = nameof(GetContributorProfile))]
        [ProducesResponseType(typeof(ContributorProfileResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [Authorize(Policy = "ApiUserOrAdmin")]
        public async Task<IActionResult> GetContributorProfile([ObjectId] string id)
        {
            if (!TryGetUserId(out var userId))
            {
                return MissingIdentity();
            }

            var result = await _mediator.Send(new GetContributorProfileQuery
            {
                CallerUserId = userId,
                IsAdministrator = User.HasClaim("scope", AdminScope),
                ContributorId = id
            });

            return result.IsFailure
                ? ToProblem(result.FailureStatusCode, "Cannot get contributor profile", result.ErrorMessage)
                : Ok(ToResponse(result.Value.Profile));
        }

        /// <summary>
        /// Replaces a contributor profile. Allowed for the contributor themselves and for administrators.
        /// Fields that are left out are cleared, so send the whole profile.
        /// </summary>
        /// <param name="id">The contributor identifier.</param>
        /// <param name="request">The role, skills, hours available and optional donor organization.</param>
        /// <returns>The updated profile.</returns>
        [HttpPut("contributor/{id}", Name = nameof(UpdateContributorProfile))]
        [ProducesResponseType(typeof(ContributorProfileResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [Authorize(Policy = "ApiUserOrAdmin")]
        public async Task<IActionResult> UpdateContributorProfile([ObjectId] string id, UpdateContributorProfileRequest request)
        {
            if (!TryGetUserId(out var userId))
            {
                return MissingIdentity();
            }

            var command = ToCommand(
                request,
                userId,
                User.FindFirst(LoginClaim)?.Value ?? string.Empty,
                User.FindFirst(AvatarUrlClaim)?.Value ?? string.Empty,
                id,
                User.HasClaim("scope", AdminScope));
            var result = await _mediator.Send(command);

            return result.IsFailure
                ? ToProblem(result.FailureStatusCode, "Cannot update contributor profile", result.ErrorMessage)
                : Ok(ToResponse(result.Value.Profile));
        }

        /// <summary>
        /// Returns the contributors affiliated with a donor organization, ordered by GitHub login. Administrators only.
        /// </summary>
        /// <param name="id">The donor organization identifier.</param>
        /// <returns>The affiliated contributors; an empty list when there are none.</returns>
        [HttpGet("donororganization/{id}/contributor", Name = nameof(GetDonorOrganizationContributors))]
        [ProducesResponseType(typeof(GetDonorOrganizationContributorsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [Authorize(Policy = "ApiAdmin")]
        public async Task<IActionResult> GetDonorOrganizationContributors([ObjectId] string id)
        {
            var result = await _mediator.Send(new GetDonorOrganizationContributorsQuery { DonorOrganizationId = id });

            return result.IsFailure
                ? ToProblem(result.FailureStatusCode, "Donor organization not found", result.ErrorMessage)
                : Ok(new GetDonorOrganizationContributorsResponse
                {
                    Contributors = result.Value.Contributors.Select(ToResponse).ToList()
                });
        }

        private static UpdateContributorProfileCommand ToCommand(
            UpdateContributorProfileRequest request,
            string userId,
            string login,
            string avatarUrl,
            string? contributorId,
            bool isAdministrator)
        {
            return new UpdateContributorProfileCommand
            {
                CallerUserId = userId,
                GithubLogin = login,
                AvatarUrl = avatarUrl,
                IsAdministrator = isAdministrator,
                ContributorId = contributorId,
                Role = request.Role,
                Skills = request.Skills,
                HoursAvailable = request.HoursAvailable,
                DonorOrganizationId = request.DonorOrganizationId
            };
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
                Detail = "The token does not contain a valid user identifier, GitHub login and avatar.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        private ObjectResult ToProblem(HttpStatusCode? statusCode, string title, string detail)
        {
            var status = statusCode switch
            {
                HttpStatusCode.NotFound => StatusCodes.Status404NotFound,
                HttpStatusCode.Forbidden => StatusCodes.Status403Forbidden,
                _ => StatusCodes.Status400BadRequest
            };

            return StatusCode(status, new ProblemDetails { Title = title, Detail = detail, Status = status });
        }

        private static ContributorProfileResponse ToResponse(ContributorProfileDto dto)
        {
            return new ContributorProfileResponse
            {
                Id = dto.Id,
                GithubLogin = dto.GithubLogin,
                AvatarUrl = dto.AvatarUrl,
                Role = dto.Role,
                Skills = dto.Skills,
                HoursAvailable = dto.HoursAvailable,
                DonorOrganizationId = dto.DonorOrganizationId
            };
        }
    }
}