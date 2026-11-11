using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Net;
using System.Net.Mime;
using TimeForCode.Donation.Api.V1.Models;
using TimeForCode.Donation.Commands;

namespace TimeForCode.Donation.Api.V1.Controllers
{
    /// <summary>
    /// Anonymous, aggregate-only reads for badges and public impact statistics. They expose hour totals, never personal data.
    /// </summary>
    [ApiController]
    [Produces(MediaTypeNames.Application.Json)]
    [Route("api/v1")]
    [EnableRateLimiting("api")]
    [AllowAnonymous]
    public class PublicStatisticsController : ControllerBase
    {
        private readonly IMediator _mediator;

        /// <summary>
        /// Initializes a new instance of the <see cref="PublicStatisticsController"/> class.
        /// </summary>
        /// <param name="mediator">The MediatR mediator.</param>
        public PublicStatisticsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Returns the total hours a contributor logged (anonymous). Only the total is exposed.
        /// </summary>
        /// <param name="id">The contributor identifier.</param>
        /// <returns>The total hours of the contributor.</returns>
        /// <response code="200">The total hours of the contributor.</response>
        /// <response code="400">The identifier is not valid.</response>
        /// <response code="404">The contributor does not exist.</response>
        /// <response code="500">The request could not be processed because of an unexpected error.</response>
        [HttpGet("contributor/{id}/hours", Name = nameof(GetContributorHours))]
        [ProducesResponseType(typeof(ContributorHoursResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetContributorHours([ObjectId] string id)
        {
            var result = await _mediator.Send(new GetContributorHoursQuery { ContributorId = id });

            return result.IsFailure
                ? ToProblem(result.FailureStatusCode, "Cannot get contributor hours", result.ErrorMessage)
                : Ok(new ContributorHoursResponse { TotalHours = result.Value.TotalHours });
        }

        /// <summary>
        /// Returns the hours logged towards the donations of a donor organization (anonymous). Only the total is exposed.
        /// </summary>
        /// <param name="id">The donor organization identifier.</param>
        /// <returns>The hours logged on the Active and Completed donations of the organization.</returns>
        /// <response code="200">The hours logged towards the donations of the organization.</response>
        /// <response code="400">The identifier is not valid.</response>
        /// <response code="404">The donor organization does not exist.</response>
        /// <response code="500">The request could not be processed because of an unexpected error.</response>
        [HttpGet("donororganization/{id}/hours", Name = nameof(GetDonorOrganizationHours))]
        [ProducesResponseType(typeof(DonorOrganizationHoursResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetDonorOrganizationHours([ObjectId] string id)
        {
            var result = await _mediator.Send(new GetDonorOrganizationHoursQuery { DonorOrganizationId = id });

            return result.IsFailure
                ? ToProblem(result.FailureStatusCode, "Cannot get donor organization hours", result.ErrorMessage)
                : Ok(new DonorOrganizationHoursResponse { HoursLogged = result.Value.HoursLogged });
        }

        /// <summary>
        /// Returns the platform-wide totals (anonymous): hours logged and donor organizations supported.
        /// </summary>
        /// <returns>The totals; zeros on a new platform.</returns>
        /// <response code="200">The platform-wide totals; zeros when no hours were logged.</response>
        /// <response code="500">The request could not be processed because of an unexpected error.</response>
        [HttpGet("statistics", Name = nameof(GetPlatformStatistics))]
        [ProducesResponseType(typeof(PlatformStatisticsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetPlatformStatistics()
        {
            var result = await _mediator.Send(new GetPlatformStatisticsQuery());

            return result.IsFailure
                ? ToProblem(result.FailureStatusCode, "Cannot get statistics", result.ErrorMessage)
                : Ok(new PlatformStatisticsResponse
                {
                    TotalHoursLogged = result.Value.TotalHoursLogged,
                    OrganizationsSupported = result.Value.OrganizationsSupported
                });
        }

        private ObjectResult ToProblem(HttpStatusCode? statusCode, string title, string detail)
        {
            var status = statusCode == HttpStatusCode.NotFound ? StatusCodes.Status404NotFound : StatusCodes.Status400BadRequest;

            return StatusCode(status, new ProblemDetails { Title = title, Detail = detail, Status = status });
        }
    }
}