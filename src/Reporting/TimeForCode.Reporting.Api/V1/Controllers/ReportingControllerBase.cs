using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Http.Headers;

namespace TimeForCode.Reporting.Api.V1.Controllers
{
    /// <summary>
    /// What the reporting controllers share: reading the caller's bearer token and answering with the status the Donation context gave.
    /// </summary>
    public abstract class ReportingControllerBase : ControllerBase
    {
        private const string BearerScheme = "Bearer";

        /// <summary>Reads the bearer access token of the request; false when it is absent or not a bearer token.</summary>
        protected bool TryGetAccessToken(out string accessToken)
        {
            accessToken = string.Empty;

            if (!AuthenticationHeaderValue.TryParse(Request.Headers.Authorization.ToString(), out var header)
                || !string.Equals(header.Scheme, BearerScheme, StringComparison.OrdinalIgnoreCase)
                || string.IsNullOrEmpty(header.Parameter))
            {
                return false;
            }

            accessToken = header.Parameter;
            return true;
        }

        /// <summary>The answer for a request without a usable bearer token.</summary>
        protected BadRequestObjectResult MissingToken()
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Access token could not be determined",
                Detail = "The request does not carry a bearer access token.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        /// <summary>Answers with the status the failure carries (401, 403, 404, 409, 502); any other failure becomes a 400.</summary>
        protected ObjectResult ToProblem(HttpStatusCode? statusCode, string title, string detail)
        {
            var status = statusCode switch
            {
                HttpStatusCode.Unauthorized => StatusCodes.Status401Unauthorized,
                HttpStatusCode.Forbidden => StatusCodes.Status403Forbidden,
                HttpStatusCode.NotFound => StatusCodes.Status404NotFound,
                HttpStatusCode.Conflict => StatusCodes.Status409Conflict,
                HttpStatusCode.BadGateway => StatusCodes.Status502BadGateway,
                _ => StatusCodes.Status400BadRequest
            };

            return StatusCode(status, new ProblemDetails { Title = title, Detail = detail, Status = status });
        }
    }
}