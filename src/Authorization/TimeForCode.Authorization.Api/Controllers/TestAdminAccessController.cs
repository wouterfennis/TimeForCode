using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using System.Net.Mime;
using TimeForCode.Authorization.Api.Models.Admin;
using TimeForCode.Authorization.Application.Options;
using TimeForCode.Authorization.Commands.Admin;
using TimeForCode.Shared.Api.Authentication.Models;

namespace TimeForCode.Authorization.Api.Controllers
{
    /// <summary>
    /// Test-only admin access for live tests on the local and CI stack. Answers 404 unless
    /// <see cref="TestAdminAccessOptions.Enabled"/> is set, which startup refuses outside Development and Test.
    /// Hidden from the OpenAPI document so the published contract does not advertise it.
    /// </summary>
    [Route("api/v1/test-admin-access")]
    [Produces(MediaTypeNames.Application.Json)]
    [ApiExplorerSettings(IgnoreApi = true)]
    [ApiController]
    public class TestAdminAccessController : ControllerBase
    {
        private readonly ISender _sender;
        private readonly TestAdminAccessOptions _options;
        private readonly ILogger<TestAdminAccessController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestAdminAccessController"/> class.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="options">The test admin access options.</param>
        /// <param name="logger">The logger.</param>
        public TestAdminAccessController(ISender sender, IOptions<TestAdminAccessOptions> options, ILogger<TestAdminAccessController> logger)
        {
            _sender = sender;
            _options = options.Value;
            _logger = logger;
        }

        /// <summary>
        /// Issues a short-lived admin token (no refresh token, marked <c>amr=test</c>) for live tests. Only available while test-only admin access is enabled.
        /// </summary>
        [HttpPost]
        [Route("token", Name = "TestAdminToken")]
        [EnableRateLimiting("admin-auth")]
        [ProducesResponseType(typeof(AccessToken), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CreateTokenAsync(TestAdminTokenRequestModel model)
        {
            if (!_options.Enabled)
            {
                return NotFound();
            }

            var result = await _sender.Send(new CreateTestAdminTokenCommand { Secret = model.Secret });
            if (result.IsFailure)
            {
                _logger.LogWarning("Test admin token refused for caller {RemoteIpAddress}", HttpContext.Connection.RemoteIpAddress);
                return Unauthorized();
            }

            return Ok(result.Value);
        }
    }
}