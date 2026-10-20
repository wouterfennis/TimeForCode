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
    /// Controller for pledging hours to projects and managing the lifecycle of those donations.
    /// </summary>
    [ApiController]
    [Produces(MediaTypeNames.Application.Json)]
    [Route("api/v1")]
    [EnableRateLimiting("api")]
    public class DonationController : ControllerBase
    {
        private const string AdminScope = "admin";

        private readonly IMediator _mediator;

        /// <summary>
        /// Initializes a new instance of the <see cref="DonationController"/> class.
        /// </summary>
        /// <param name="mediator">The MediatR mediator.</param>
        public DonationController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Pledges hours to an active project, as an individual or for a donor organization (never both). The donation starts as Pending.
        /// </summary>
        /// <param name="id">The project identifier.</param>
        /// <param name="request">The hours and the donor.</param>
        /// <returns>The created donation.</returns>
        [HttpPost("project/{id}/donation", Name = nameof(PledgeDonation))]
        [ProducesResponseType(typeof(DonationResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [Authorize(Policy = "ApiUser")]
        public async Task<IActionResult> PledgeDonation([ObjectId] string id, PledgeDonationRequest request)
        {
            if (!TryGetUserId(out var userId))
            {
                return MissingIdentity();
            }

            var result = await _mediator.Send(new PledgeDonationCommand
            {
                ProjectId = id,
                UserId = userId,
                Hours = request.Hours,
                AsIndividual = request.AsIndividual,
                DonorOrganizationId = request.DonorOrganizationId
            });

            if (result.IsFailure)
            {
                return ToProblem(result.FailureStatusCode, "Cannot pledge hours", result.ErrorMessage);
            }

            return CreatedAtAction(nameof(GetProjectDonations), new { id }, ToResponse(result.Value.Donation));
        }

        /// <summary>
        /// Returns the donations pledged to an active project (anonymous), oldest first, with their progress.
        /// </summary>
        /// <param name="id">The project identifier.</param>
        /// <returns>The donations of the project; an empty list when there are none.</returns>
        [HttpGet("project/{id}/donation", Name = nameof(GetProjectDonations))]
        [ProducesResponseType(typeof(GetDonationsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [AllowAnonymous]
        public async Task<IActionResult> GetProjectDonations([ObjectId] string id)
        {
            var result = await _mediator.Send(new GetProjectDonationsQuery { ProjectId = id });

            if (result.IsFailure)
            {
                return ToProblem(result.FailureStatusCode, "Project not found", result.ErrorMessage);
            }

            return Ok(ToResponse(result.Value));
        }

        /// <summary>
        /// Returns the donations the caller pledged, oldest first, with their progress.
        /// </summary>
        /// <returns>The caller's donations; an empty list when there are none.</returns>
        [HttpGet("donation", Name = nameof(GetMyDonations))]
        [ProducesResponseType(typeof(GetDonationsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [Authorize(Policy = "ApiUser")]
        public async Task<IActionResult> GetMyDonations()
        {
            if (!TryGetUserId(out var userId))
            {
                return MissingIdentity();
            }

            var result = await _mediator.Send(new GetMyDonationsQuery { UserId = userId });

            return Ok(ToResponse(result.Value));
        }

        /// <summary>
        /// Activates a pending donation. Allowed for the donor who pledged it and for administrators.
        /// </summary>
        /// <param name="id">The donation identifier.</param>
        /// <returns>The activated donation.</returns>
        [HttpPost("donation/{id}/activate", Name = nameof(ActivateDonation))]
        [ProducesResponseType(typeof(DonationResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [Authorize(Policy = "ApiUserOrAdmin")]
        public async Task<IActionResult> ActivateDonation([ObjectId] string id)
        {
            if (!TryGetUserId(out var userId))
            {
                return MissingIdentity();
            }

            var result = await _mediator.Send(new ActivateDonationCommand
            {
                DonationId = id,
                UserId = userId,
                IsAdministrator = User.HasClaim("scope", AdminScope)
            });

            return result.IsFailure
                ? ToProblem(result.FailureStatusCode, "Cannot activate donation", result.ErrorMessage)
                : Ok(ToResponse(result.Value.Donation));
        }

        /// <summary>
        /// Cancels a pending or active donation. Allowed for the donor who pledged it and for administrators.
        /// </summary>
        /// <param name="id">The donation identifier.</param>
        /// <returns>The cancelled donation.</returns>
        [HttpPost("donation/{id}/cancel", Name = nameof(CancelDonation))]
        [ProducesResponseType(typeof(DonationResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [Authorize(Policy = "ApiUserOrAdmin")]
        public async Task<IActionResult> CancelDonation([ObjectId] string id)
        {
            if (!TryGetUserId(out var userId))
            {
                return MissingIdentity();
            }

            var result = await _mediator.Send(new CancelDonationCommand
            {
                DonationId = id,
                UserId = userId,
                IsAdministrator = User.HasClaim("scope", AdminScope)
            });

            return result.IsFailure
                ? ToProblem(result.FailureStatusCode, "Cannot cancel donation", result.ErrorMessage)
                : Ok(ToResponse(result.Value.Donation));
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
            var status = statusCode switch
            {
                HttpStatusCode.NotFound => StatusCodes.Status404NotFound,
                HttpStatusCode.Forbidden => StatusCodes.Status403Forbidden,
                HttpStatusCode.Conflict => StatusCodes.Status409Conflict,
                _ => StatusCodes.Status400BadRequest
            };

            return StatusCode(status, new ProblemDetails { Title = title, Detail = detail, Status = status });
        }

        private static GetDonationsResponse ToResponse(GetDonationsResult result)
        {
            return new GetDonationsResponse { Donations = result.Donations.Select(ToResponse).ToList() };
        }

        private static DonationResponse ToResponse(DonationDto dto)
        {
            return new DonationResponse
            {
                Id = dto.Id,
                ProjectId = dto.ProjectId,
                DonorOrganizationId = dto.DonorOrganizationId,
                Status = dto.Status,
                HoursCommitted = dto.HoursCommitted,
                HoursLogged = dto.HoursLogged,
                ProgressPercentage = dto.ProgressPercentage,
                CreatedAt = dto.CreatedAt,
                UpdatedAt = dto.UpdatedAt
            };
        }
    }
}