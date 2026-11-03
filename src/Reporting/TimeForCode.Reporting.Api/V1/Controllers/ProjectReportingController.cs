using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Net.Mime;
using TimeForCode.Reporting.Api.V1.Models;
using TimeForCode.Reporting.Commands;
using TimeForCode.Shared.Api.Extensions;

namespace TimeForCode.Reporting.Api.V1.Controllers
{
    /// <summary>
    /// Controller for the donation report of a project: the detailed report for its maintainer and administrators, and a public
    /// summary with aggregate totals only. The data is read from the Donation context.
    /// </summary>
    [ApiController]
    [Produces(MediaTypeNames.Application.Json)]
    [Route("api/v1/reporting/project/{id}")]
    [EnableRateLimiting("api")]
    public class ProjectReportingController : ReportingControllerBase
    {
        private const string AdminScope = "admin";

        private readonly IMediator _mediator;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectReportingController"/> class.
        /// </summary>
        /// <param name="mediator">The MediatR mediator.</param>
        public ProjectReportingController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Returns the detailed donation report of a project: hours received and logged, progress, the donations with their statuses and
        /// the contributors with their hours. Allowed for the maintainer of the project and for administrators.
        /// </summary>
        /// <remarks>
        /// Only Active and Completed donations count as received; Pending and Cancelled ones are listed and counted per status.
        /// Only published projects can be reported on; an unpublished project answers 404, also for its maintainer.
        /// </remarks>
        /// <param name="id">The project identifier.</param>
        /// <returns>The detailed report; zeros and empty lists when the project has no donations or contributions.</returns>
        [HttpGet("donation-report", Name = nameof(GetDonationReport))]
        [ProducesResponseType(typeof(ProjectDonationReportResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway)]
        [Authorize(Policy = "ApiUserOrAdmin")]
        public async Task<IActionResult> GetDonationReport([ObjectId] string id)
        {
            if (!TryGetAccessToken(out var accessToken))
            {
                return MissingToken();
            }

            var result = await _mediator.Send(new GetProjectDonationReportQuery
            {
                ProjectId = id,
                AccessToken = accessToken,
                IsAdministrator = User.HasClaim(AuthenticationExtensions.ScopeClaim, AdminScope)
            });

            return result.IsFailure
                ? ToProblem(result.FailureStatusCode, "Cannot get donation report", result.ErrorMessage)
                : Ok(new ProjectDonationReportResponse
                {
                    HoursReceived = result.Value.HoursReceived,
                    HoursLogged = result.Value.HoursLogged,
                    ProgressPercentage = result.Value.ProgressPercentage,
                    DonorCount = result.Value.DonorCount,
                    ContributorCount = result.Value.ContributorCount,
                    PendingDonations = result.Value.PendingDonations,
                    ActiveDonations = result.Value.ActiveDonations,
                    CompletedDonations = result.Value.CompletedDonations,
                    CancelledDonations = result.Value.CancelledDonations,
                    Donations = result.Value.Donations
                        .Select(d => new ProjectDonationResponse
                        {
                            Id = d.Id,
                            DonorOrganizationId = d.DonorOrganizationId,
                            Status = d.Status,
                            HoursCommitted = d.HoursCommitted,
                            HoursLogged = d.HoursLogged,
                            CreatedAt = d.CreatedAt
                        })
                        .ToList(),
                    Contributors = result.Value.Contributors
                        .Select(c => new ProjectContributorResponse { ContributorId = c.ContributorId, ContributorLogin = c.ContributorLogin, Hours = c.Hours })
                        .ToList()
                });
        }

        /// <summary>
        /// Returns the public summary of a project (anonymous): aggregate totals only, without any personal data.
        /// </summary>
        /// <param name="id">The project identifier.</param>
        /// <returns>Hours received and logged, the progress and the number of donors and contributors.</returns>
        [HttpGet("public-summary", Name = nameof(GetPublicSummary))]
        [ProducesResponseType(typeof(ProjectPublicSummaryResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway)]
        [AllowAnonymous]
        [EnableRateLimiting("public")]
        public async Task<IActionResult> GetPublicSummary([ObjectId] string id)
        {
            var result = await _mediator.Send(new GetProjectPublicSummaryQuery { ProjectId = id });

            return result.IsFailure
                ? ToProblem(result.FailureStatusCode, "Cannot get public summary", result.ErrorMessage)
                : Ok(new ProjectPublicSummaryResponse
                {
                    HoursReceived = result.Value.HoursReceived,
                    HoursLogged = result.Value.HoursLogged,
                    ProgressPercentage = result.Value.ProgressPercentage,
                    DonorCount = result.Value.DonorCount,
                    ContributorCount = result.Value.ContributorCount
                });
        }
    }
}