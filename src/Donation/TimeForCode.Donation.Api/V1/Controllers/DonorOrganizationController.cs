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
    /// Controller for managing donor organization operations.
    /// </summary>
    [ApiController]
    [Produces(MediaTypeNames.Application.Json)]
    [Route("api/v1/[controller]")]
    [EnableRateLimiting("api")]
    [AllowAnonymous]
    public class DonorOrganizationController : ControllerBase
    {
        private readonly IMediator _mediator;

        /// <summary>
        /// Initializes a new instance of the <see cref="DonorOrganizationController"/> class.
        /// </summary>
        /// <param name="mediator">The MediatR mediator.</param>
        public DonorOrganizationController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Creates a new donor organization.
        /// </summary>
        /// <param name="request">The creation request.</param>
        /// <returns>The created donor organization.</returns>
        [HttpPost(Name = nameof(CreateDonorOrganization))]
        [ProducesResponseType(typeof(DonorOrganizationResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateDonorOrganization(CreateDonorOrganizationRequest request)
        {
            var command = new CreateDonorOrganizationCommand
            {
                Name = request.Name,
                ContactEmail = request.ContactEmail,
                Website = request.Website
            };

            var result = await _mediator.Send(command);

            if (result.IsFailure)
            {
                if (result.FailureStatusCode == HttpStatusCode.Conflict)
                {
                    return Conflict(new ProblemDetails
                    {
                        Title = "Donor organization already exists",
                        Detail = result.ErrorMessage,
                        Status = StatusCodes.Status409Conflict
                    });
                }

                return BadRequest(new ProblemDetails
                {
                    Title = "Cannot create donor organization",
                    Detail = result.ErrorMessage,
                    Status = StatusCodes.Status400BadRequest
                });
            }

            var response = MapToResponse(result.Value.Organization);
            return CreatedAtAction(nameof(GetDonorOrganizationById), new { id = response.Id }, response);
        }

        /// <summary>
        /// Returns a paginated list of all donor organizations.
        /// </summary>
        /// <param name="pageNumber">The page number (1-based, default 1).</param>
        /// <param name="pageSize">The number of items per page (default 20).</param>
        /// <returns>A paginated list of donor organizations.</returns>
        [HttpGet(Name = nameof(GetDonorOrganizations))]
        [ProducesResponseType(typeof(GetDonorOrganizationsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetDonorOrganizations([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
        {
            if (pageNumber < 1)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid page number",
                    Detail = "Page number must be greater than or equal to 1.",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            if (pageSize < 1 || pageSize > 100)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid page size",
                    Detail = "Page size must be between 1 and 100.",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            var query = new GetDonorOrganizationsQuery { PageNumber = pageNumber, PageSize = pageSize };
            var result = await _mediator.Send(query);

            if (result.IsFailure)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Could not retrieve donor organizations",
                    Detail = result.ErrorMessage,
                    Status = StatusCodes.Status400BadRequest
                });
            }

            var response = new GetDonorOrganizationsResponse
            {
                Organizations = result.Value.Organizations.Select(MapToResponse).ToList(),
                TotalCount = result.Value.TotalCount,
                PageNumber = result.Value.PageNumber,
                PageSize = result.Value.PageSize
            };

            return Ok(response);
        }

        /// <summary>
        /// Returns the full details of a donor organization.
        /// </summary>
        /// <param name="id">The donor organization identifier.</param>
        /// <returns>Full donor organization details.</returns>
        [HttpGet("{id}", Name = nameof(GetDonorOrganizationById))]
        [ProducesResponseType(typeof(DonorOrganizationResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetDonorOrganizationById(string id)
        {
            var query = new GetDonorOrganizationByIdQuery { Id = id };
            var result = await _mediator.Send(query);

            if (result.IsFailure)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Donor organization not found",
                    Detail = result.ErrorMessage,
                    Status = StatusCodes.Status404NotFound
                });
            }

            return Ok(MapToResponse(result.Value.Organization));
        }

        /// <summary>
        /// Updates an existing donor organization.
        /// </summary>
        /// <param name="id">The donor organization identifier.</param>
        /// <param name="request">The update request.</param>
        /// <returns>The updated donor organization.</returns>
        [HttpPut("{id}", Name = nameof(UpdateDonorOrganization))]
        [ProducesResponseType(typeof(DonorOrganizationResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> UpdateDonorOrganization(string id, UpdateDonorOrganizationRequest request)
        {
            var command = new UpdateDonorOrganizationCommand
            {
                Id = id,
                Name = request.Name,
                ContactEmail = request.ContactEmail,
                Website = request.Website
            };

            var result = await _mediator.Send(command);

            if (result.IsFailure)
            {
                if (result.FailureStatusCode == HttpStatusCode.Conflict)
                {
                    return Conflict(new ProblemDetails
                    {
                        Title = "Donor organization name conflict",
                        Detail = result.ErrorMessage,
                        Status = StatusCodes.Status409Conflict
                    });
                }

                return NotFound(new ProblemDetails
                {
                    Title = "Donor organization not found",
                    Detail = result.ErrorMessage,
                    Status = StatusCodes.Status404NotFound
                });
            }

            return Ok(MapToResponse(result.Value.Organization));
        }

        /// <summary>
        /// Deletes a donor organization.
        /// </summary>
        /// <param name="id">The donor organization identifier.</param>
        /// <returns>No content on success.</returns>
        [HttpDelete("{id}", Name = nameof(DeleteDonorOrganization))]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteDonorOrganization(string id)
        {
            var command = new DeleteDonorOrganizationCommand { Id = id };
            var result = await _mediator.Send(command);

            if (result.IsFailure)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Donor organization not found",
                    Detail = result.ErrorMessage,
                    Status = StatusCodes.Status404NotFound
                });
            }

            return NoContent();
        }

        private static DonorOrganizationResponse MapToResponse(DonorOrganizationDto dto)
        {
            return new DonorOrganizationResponse
            {
                Id = dto.Id,
                Name = dto.Name,
                ContactEmail = dto.ContactEmail,
                Website = dto.Website,
                CreatedAt = dto.CreatedAt,
                UpdatedAt = dto.UpdatedAt
            };
        }
    }
}