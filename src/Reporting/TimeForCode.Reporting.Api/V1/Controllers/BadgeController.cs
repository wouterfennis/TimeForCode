using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.ComponentModel.DataAnnotations;
using System.Net.Mime;
using TimeForCode.Reporting.Api.V1.Models;
using TimeForCode.Reporting.Commands;
using TimeForCode.Reporting.Values;

namespace TimeForCode.Reporting.Api.V1.Controllers
{
    /// <summary>
    /// Badges and milestone recognition: public reads of the badges and the impact statistics, and the administrator-only
    /// configuration of the badge tiers. A badge is awarded when the badges are requested and the recipient has reached a tier,
    /// at most once per tier and recipient.
    /// </summary>
    [ApiController]
    [Produces(MediaTypeNames.Application.Json)]
    [Route("api/v1/reporting")]
    public class BadgeController : ReportingControllerBase
    {
        private readonly IMediator _mediator;

        /// <summary>
        /// Initializes a new instance of the <see cref="BadgeController"/> class.
        /// </summary>
        /// <param name="mediator">The MediatR mediator.</param>
        public BadgeController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Returns the badges of a donor organization (anonymous), awarding the ones it has newly earned.
        /// </summary>
        /// <remarks>
        /// An organization is measured on the hours logged on its Active and Completed donations and earns the badges of the
        /// tiers with recipient type <c>Organization</c>.
        /// </remarks>
        /// <param name="id">The donor organization identifier.</param>
        /// <returns>The badges, earliest award first; an empty list when none was earned.</returns>
        /// <response code="200">The badges of the organization; an empty list when none was earned.</response>
        /// <response code="400">The organization identifier is not a valid identifier.</response>
        /// <response code="404">The donor organization does not exist.</response>
        /// <response code="429">Too many requests.</response>
        /// <response code="500">The badges could not be determined because of an unexpected error.</response>
        /// <response code="502">The Donation API could not be reached or answered with an error.</response>
        [HttpGet("donororganization/{id}/badges", Name = nameof(GetOrganizationBadges))]
        [ProducesResponseType(typeof(BadgesResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway)]
        [AllowAnonymous]
        [EnableRateLimiting("public")]
        public async Task<IActionResult> GetOrganizationBadges([ObjectId] string id)
        {
            var result = await _mediator.Send(new GetOrganizationBadgesQuery { DonorOrganizationId = id });

            return result.IsFailure
                ? ToProblem(result.FailureStatusCode, "Cannot get badges of donor organization", result.ErrorMessage)
                : Ok(ToResponse(result.Value));
        }

        /// <summary>
        /// Returns the badges of a contributor (anonymous), awarding the ones they have newly earned.
        /// </summary>
        /// <remarks>
        /// A contributor is measured on the total hours they logged themselves and earns the badges of the tiers with recipient
        /// type <c>Contributor</c>. Only badge names and award dates are exposed, no other personal data.
        /// </remarks>
        /// <param name="id">The contributor identifier.</param>
        /// <returns>The badges, earliest award first; an empty list when none was earned.</returns>
        /// <response code="200">The badges of the contributor; an empty list when none was earned.</response>
        /// <response code="400">The contributor identifier is not a valid identifier.</response>
        /// <response code="404">The contributor does not exist.</response>
        /// <response code="429">Too many requests.</response>
        /// <response code="500">The badges could not be determined because of an unexpected error.</response>
        /// <response code="502">The Donation API could not be reached or answered with an error.</response>
        [HttpGet("contributor/{id}/badges", Name = nameof(GetContributorBadges))]
        [ProducesResponseType(typeof(BadgesResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway)]
        [AllowAnonymous]
        [EnableRateLimiting("public")]
        public async Task<IActionResult> GetContributorBadges([ObjectId] string id)
        {
            var result = await _mediator.Send(new GetContributorBadgesQuery { ContributorId = id });

            return result.IsFailure
                ? ToProblem(result.FailureStatusCode, "Cannot get badges of contributor", result.ErrorMessage)
                : Ok(ToResponse(result.Value));
        }

        /// <summary>
        /// Returns the public impact statistics (anonymous): the total hours logged and the number of donor organizations supported.
        /// </summary>
        /// <returns>The totals; zeros on a platform without logged hours.</returns>
        /// <response code="200">The totals; zeros on a platform without logged hours.</response>
        /// <response code="429">Too many requests.</response>
        /// <response code="500">The statistics could not be determined because of an unexpected error.</response>
        /// <response code="502">The Donation API could not be reached or answered with an error.</response>
        [HttpGet("statistics", Name = nameof(GetPublicStatistics))]
        [ProducesResponseType(typeof(PublicStatisticsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway)]
        [AllowAnonymous]
        [EnableRateLimiting("public")]
        public async Task<IActionResult> GetPublicStatistics()
        {
            var result = await _mediator.Send(new GetPublicStatisticsQuery());

            return result.IsFailure
                ? ToProblem(result.FailureStatusCode, "Cannot get statistics", result.ErrorMessage)
                : Ok(new PublicStatisticsResponse
                {
                    TotalHoursLogged = result.Value.TotalHoursLogged,
                    OrganizationsSupported = result.Value.OrganizationsSupported
                });
        }

        /// <summary>
        /// Returns the configured badge tiers (anonymous), so the thresholds can be shown next to the badges.
        /// </summary>
        /// <param name="recipientType">Only the tiers of this recipient type; all tiers when omitted.</param>
        /// <returns>The tiers ordered by recipient type and threshold; an empty list when none is configured.</returns>
        /// <response code="200">The tiers; an empty list when none is configured.</response>
        /// <response code="400">The recipient type is not valid.</response>
        /// <response code="429">Too many requests.</response>
        /// <response code="500">The tiers could not be read because of an unexpected error.</response>
        [HttpGet("badge-tier", Name = nameof(GetBadgeTiers))]
        [ProducesResponseType(typeof(BadgeTiersResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [AllowAnonymous]
        [EnableRateLimiting("public")]
        public async Task<IActionResult> GetBadgeTiers([FromQuery] BadgeRecipientType? recipientType = null)
        {
            var result = await _mediator.Send(new GetBadgeTiersQuery { RecipientType = recipientType });

            return result.IsFailure
                ? ToProblem(result.FailureStatusCode, "Cannot get badge tiers", result.ErrorMessage)
                : Ok(new BadgeTiersResponse { Tiers = result.Value.Select(ToResponse).ToList() });
        }

        /// <summary>
        /// Defines a badge tier, or changes the threshold of the tier with the same recipient type and name (administrators only).
        /// </summary>
        /// <remarks>
        /// Badges already awarded stay when a threshold is raised. A tier added or lowered is awarded to recipients who already
        /// qualify the next time their badges are requested.
        /// </remarks>
        /// <param name="recipientType">Who the tier is for: <c>Organization</c> or <c>Contributor</c>.</param>
        /// <param name="name">The tier name, 1 to 100 characters, unique per recipient type.</param>
        /// <param name="request">The threshold.</param>
        /// <returns>The tier as saved.</returns>
        /// <response code="200">The tier as saved.</response>
        /// <response code="400">The name is empty or longer than 100 characters, or the threshold is not a positive number of hours.</response>
        /// <response code="401">The access token is missing, invalid or expired.</response>
        /// <response code="403">The caller is not an administrator.</response>
        /// <response code="409">Another tier of the recipient type already has the threshold.</response>
        /// <response code="500">The tier could not be saved because of an unexpected error.</response>
        [HttpPut("badge-tier/{recipientType}/{name}", Name = nameof(DefineBadgeTier))]
        [ProducesResponseType(typeof(BadgeTierResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [Authorize(Policy = "ApiAdmin")]
        [EnableRateLimiting("api")]
        public async Task<IActionResult> DefineBadgeTier(BadgeRecipientType recipientType, [StringLength(ApiConstraints.MaxTierNameLength, MinimumLength = 1)] string name, [FromBody] DefineBadgeTierRequest request)
        {
            var result = await _mediator.Send(new DefineBadgeTierCommand { RecipientType = recipientType, Name = name, ThresholdHours = request.ThresholdHours });

            return result.IsFailure
                ? ToProblem(result.FailureStatusCode, "Cannot define badge tier", result.ErrorMessage)
                : Ok(ToResponse(result.Value));
        }

        /// <summary>
        /// Removes a badge tier (administrators only). Badges already awarded for it stay.
        /// </summary>
        /// <param name="recipientType">Who the tier is for: <c>Organization</c> or <c>Contributor</c>.</param>
        /// <param name="name">The tier name.</param>
        /// <returns>No content.</returns>
        /// <response code="204">The tier was removed.</response>
        /// <response code="401">The access token is missing, invalid or expired.</response>
        /// <response code="403">The caller is not an administrator.</response>
        /// <response code="404">There is no such tier.</response>
        /// <response code="500">The tier could not be removed because of an unexpected error.</response>
        [HttpDelete("badge-tier/{recipientType}/{name}", Name = nameof(RemoveBadgeTier))]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [Authorize(Policy = "ApiAdmin")]
        [EnableRateLimiting("api")]
        public async Task<IActionResult> RemoveBadgeTier(BadgeRecipientType recipientType, [StringLength(ApiConstraints.MaxTierNameLength, MinimumLength = 1)] string name)
        {
            var result = await _mediator.Send(new RemoveBadgeTierCommand { RecipientType = recipientType, Name = name });

            return result.IsFailure
                ? ToProblem(result.FailureStatusCode, "Cannot remove badge tier", result.ErrorMessage)
                : NoContent();
        }

        private static BadgesResponse ToResponse(IReadOnlyList<BadgeDto> badges) =>
            new() { Badges = badges.Select(b => new BadgeResponse { Name = b.Name, AwardedAt = b.AwardedAt }).ToList() };

        private static BadgeTierResponse ToResponse(BadgeTierDto tier) =>
            new() { Name = tier.Name, RecipientType = tier.RecipientType, ThresholdHours = tier.ThresholdHours };
    }
}