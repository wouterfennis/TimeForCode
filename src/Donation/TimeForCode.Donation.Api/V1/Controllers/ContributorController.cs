using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Net.Mime;
using TimeForCode.Donation.Commands;

namespace TimeForCode.Donation.Api.V1.Controllers
{
    /// <summary>
    /// Controller for the contributors of the platform; called by other services, not by end users.
    /// </summary>
    [ApiController]
    [Produces(MediaTypeNames.Application.Json)]
    [Route("api/v1/contributor")]
    [EnableRateLimiting("api")]
    [Authorize(Policy = "ApiService")]
    public class ContributorController : ControllerBase
    {
        private readonly IMediator _mediator;

        /// <summary>
        /// Initializes a new instance of the <see cref="ContributorController"/> class.
        /// </summary>
        /// <param name="mediator">The MediatR mediator.</param>
        public ContributorController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Replaces the identity of a contributor, and on their contributions, with a generic deleted user; the hours are kept. Accepted when the user has no contributions or was already anonymised.
        /// </summary>
        /// <param name="userId">The platform user identifier of the deleted account.</param>
        /// <returns>No content on success.</returns>
        [HttpPost("{userId}/anonymise", Name = nameof(AnonymiseContributor))]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> AnonymiseContributor([ObjectId] string userId)
        {
            await _mediator.Send(new AnonymiseContributorCommand { UserId = userId });

            return NoContent();
        }
    }
}