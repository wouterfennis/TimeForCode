using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using TimeForCode.Reporting.Api.V1.Rendering;
using TimeForCode.Reporting.Commands;

namespace TimeForCode.Reporting.Api.V1.Controllers
{
    /// <summary>
    /// Embeddable badge images (SVG) for a README: the hours a project received and its progress, and the highest tier of a donor
    /// organization or a contributor. Anonymous. Every answer is an image with status 200: an unknown, malformed or unpublished
    /// target shows "not found", a recipient without a badge shows "no badge yet" and a Donation API failure shows "unavailable",
    /// so a README never shows a broken image. Only totals and tier names appear, never names, logins or identifiers.
    /// </summary>
    [ApiController]
    [Route("api/v1/reporting")]
    public class BadgeImageController : ReportingControllerBase
    {
        private const string SvgContentType = "image/svg+xml";

        private readonly IMediator _mediator;
        private readonly BadgeImageOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="BadgeImageController"/> class.
        /// </summary>
        /// <param name="mediator">The MediatR mediator.</param>
        /// <param name="options">How long badge images may be cached.</param>
        public BadgeImageController(IMediator mediator, IOptions<BadgeImageOptions> options)
        {
            _mediator = mediator;
            _options = options.Value;
        }

        /// <summary>
        /// The badge with the hours a published project received.
        /// </summary>
        /// <param name="id">The project identifier.</param>
        /// <response code="200">An SVG image: the hours received, or "not found" for an unknown, malformed or unpublished project, or "unavailable".</response>
        /// <response code="429">Too many requests.</response>
        [HttpGet("project/{id}/badge/hours-received.svg", Name = nameof(GetProjectHoursReceivedBadge))]
        [Produces(SvgContentType)]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK, SvgContentType)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [AllowAnonymous]
        [EnableRateLimiting("badge")]
        public Task<IActionResult> GetProjectHoursReceivedBadge(string id) => ProjectBadgeAsync(id, ProjectBadgeKind.HoursReceived);

        /// <summary>
        /// The badge with the progress of a published project: the hours logged out of the hours received, with a percentage.
        /// </summary>
        /// <param name="id">The project identifier.</param>
        /// <response code="200">An SVG image: the progress, or "not found" for an unknown, malformed or unpublished project, or "unavailable".</response>
        /// <response code="429">Too many requests.</response>
        [HttpGet("project/{id}/badge/progress.svg", Name = nameof(GetProjectProgressBadge))]
        [Produces(SvgContentType)]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK, SvgContentType)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [AllowAnonymous]
        [EnableRateLimiting("badge")]
        public Task<IActionResult> GetProjectProgressBadge(string id) => ProjectBadgeAsync(id, ProjectBadgeKind.Progress);

        /// <summary>
        /// The badge with the highest tier of a donor organization.
        /// </summary>
        /// <param name="id">The donor organization identifier.</param>
        /// <response code="200">An SVG image: the highest tier, "no badge yet", "not found" for an unknown or malformed organization, or "unavailable".</response>
        /// <response code="429">Too many requests.</response>
        [HttpGet("donororganization/{id}/badge.svg", Name = nameof(GetOrganizationBadge))]
        [Produces(SvgContentType)]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK, SvgContentType)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [AllowAnonymous]
        [EnableRateLimiting("badge")]
        public Task<IActionResult> GetOrganizationBadge(string id) => ImageOfAsync(new GetOrganizationBadgeImageQuery { DonorOrganizationId = id });

        /// <summary>
        /// The badge with the highest tier of a contributor. An unknown or malformed contributor shows the same image as a
        /// contributor without badges, so the badge does not confirm that an identifier exists.
        /// </summary>
        /// <param name="id">The contributor identifier.</param>
        /// <response code="200">An SVG image: the highest tier, "no badge yet", or "unavailable".</response>
        /// <response code="429">Too many requests.</response>
        [HttpGet("contributor/{id}/badge.svg", Name = nameof(GetContributorBadge))]
        [Produces(SvgContentType)]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK, SvgContentType)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [AllowAnonymous]
        [EnableRateLimiting("badge")]
        public Task<IActionResult> GetContributorBadge(string id) => ImageOfAsync(new GetContributorBadgeImageQuery { ContributorId = id });

        private Task<IActionResult> ProjectBadgeAsync(string id, ProjectBadgeKind kind) => ImageOfAsync(new GetProjectBadgeImageQuery { ProjectId = id, Kind = kind });

        private async Task<IActionResult> ImageOfAsync(IRequest<Result<BadgeImageContent>> query)
        {
            Result<BadgeImageContent> content = await _mediator.Send(query);

            // The handlers always succeed; should one ever fail, the README still gets an image.
            return Image(content.IsSuccess ? content.Value : new BadgeImageContent(BadgeImageState.Unavailable));
        }

        private ContentResult Image(BadgeImageContent content)
        {
            int seconds = content.State == BadgeImageState.Unavailable ? _options.UnavailableCacheSeconds : _options.CacheSeconds;
            Response.Headers.CacheControl = $"public, max-age={seconds}";

            return Content(SvgBadgeRenderer.Render(content), SvgContentType);
        }
    }
}