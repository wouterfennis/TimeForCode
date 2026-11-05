using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using TimeForCode.Reporting.Api.V1.Models;
using TimeForCode.Reporting.Commands;
using TimeForCode.Reporting.Values;

namespace TimeForCode.Reporting.Api.V1.Controllers
{
    /// <summary>
    /// Controller for the impact report and the contribution history export of a donor organization.
    /// The data is read from the Donation context with the caller's own access token, so the Donation context decides who may read it:
    /// contributors affiliated with the organization and administrators.
    /// </summary>
    [ApiController]
    [Produces(MediaTypeNames.Application.Json)]
    [Route("api/v1/reporting/donororganization/{id}")]
    [EnableRateLimiting("api")]
    public class ReportingController : ReportingControllerBase
    {
        private readonly IMediator _mediator;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReportingController"/> class.
        /// </summary>
        /// <param name="mediator">The MediatR mediator.</param>
        public ReportingController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Returns the impact report of a donor organization: hours pledged and logged, the number of projects supported and the breakdown per project.
        /// </summary>
        /// <remarks>
        /// Only Active and Completed donations count: a Pending donation is not yet committed and a Cancelled one no longer is.
        /// </remarks>
        /// <param name="id">The donor organization identifier.</param>
        /// <returns>The impact report; zeros and an empty breakdown when the organization has no counted donations.</returns>
        /// <response code="200">The impact report; zeros and an empty breakdown when the organization has no counted donations.</response>
        /// <response code="400">The organization identifier is not a valid identifier.</response>
        /// <response code="401">The access token is missing, invalid or expired.</response>
        /// <response code="403">The caller is neither affiliated with the organization nor an administrator.</response>
        /// <response code="404">The donor organization does not exist.</response>
        /// <response code="500">The report could not be built because of an unexpected error.</response>
        /// <response code="502">The Donation API could not be reached or answered with an error.</response>
        [HttpGet("impact-report", Name = nameof(GetImpactReport))]
        [ProducesResponseType(typeof(ImpactReportResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway)]
        [Authorize(Policy = "ApiUserOrAdmin")]
        public async Task<IActionResult> GetImpactReport([ObjectId] string id)
        {
            if (!TryGetAccessToken(out var accessToken))
            {
                return MissingToken();
            }

            var result = await _mediator.Send(new GetImpactReportQuery { DonorOrganizationId = id, AccessToken = accessToken });

            return result.IsFailure
                ? ToProblem(result.FailureStatusCode, "Cannot get impact report", result.ErrorMessage)
                : Ok(new ImpactReportResponse
                {
                    HoursPledged = result.Value.HoursPledged,
                    HoursLogged = result.Value.HoursLogged,
                    ProjectsSupported = result.Value.ProjectsSupported,
                    Projects = result.Value.Projects
                        .Select(p => new ProjectImpactResponse { ProjectId = p.ProjectId, HoursPledged = p.HoursPledged, HoursLogged = p.HoursLogged })
                        .ToList()
                });
        }

        /// <summary>
        /// Exports the contribution history of a donor organization as a CSV or JSON file, earliest contribution first.
        /// </summary>
        /// <remarks>
        /// CSV columns: ContributionId, ProjectId, ContributorLogin, Hours, GithubReference, LoggedAt (UTC, ISO 8601). JSON has the same
        /// values as camel case fields in a <c>contributions</c> list. Contributions are attributed by the contributor's current
        /// affiliation, so a contributor who moves to another organization takes their history along. Deleted contributions are left out.
        /// </remarks>
        /// <param name="id">The donor organization identifier.</param>
        /// <param name="format">The file format, <c>Json</c> (default) or <c>Csv</c>.</param>
        /// <returns>The export as a file download.</returns>
        /// <response code="200">The export as a file download in the requested format.</response>
        /// <response code="400">The organization identifier or the format is not valid.</response>
        /// <response code="401">The access token is missing, invalid or expired.</response>
        /// <response code="403">The caller is neither affiliated with the organization nor an administrator.</response>
        /// <response code="404">The donor organization does not exist.</response>
        /// <response code="500">The export could not be built because of an unexpected error.</response>
        /// <response code="502">The Donation API could not be reached or answered with an error.</response>
        [HttpGet("contribution-history", Name = nameof(GetContributionHistory))]
        [ProducesResponseType(typeof(ContributionHistoryResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway)]
        [Authorize(Policy = "ApiUserOrAdmin")]
        public async Task<IActionResult> GetContributionHistory([ObjectId] string id, [FromQuery] ExportFormat format = ExportFormat.Json)
        {
            if (!TryGetAccessToken(out var accessToken))
            {
                return MissingToken();
            }

            var result = await _mediator.Send(new GetContributionHistoryExportQuery { DonorOrganizationId = id, AccessToken = accessToken, Format = format });

            return result.IsFailure
                ? ToProblem(result.FailureStatusCode, "Cannot export contribution history", result.ErrorMessage)
                : File(Encoding.UTF8.GetBytes(result.Value.Content), result.Value.ContentType, result.Value.FileName);
        }
    }
}