using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace TimeForCode.Frontend.Bff.Sessions
{
    /// <summary>
    /// The Authorization API being down, slow or answering nonsense is a failure of the upstream, not of the browser's request:
    /// the answer is a <c>502</c> ProblemDetails without the reason, never an empty 500 or a stack trace.
    /// </summary>
    internal sealed class UpstreamFailureHandler(ILogger<UpstreamFailureHandler> logger) : IExceptionHandler
    {
        /// <summary>True for an exception the Authorization API client throws when the call itself failed (not when the browser went away).</summary>
        public static bool IsUpstreamFailure(Exception exception, CancellationToken requestAborted) =>
            !requestAborted.IsCancellationRequested
            && exception is HttpRequestException or TaskCanceledException or TimeoutException or InvalidOperationException;

        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            if (!IsUpstreamFailure(exception, httpContext.RequestAborted))
            {
                return false;
            }

            logger.LogError(exception, "The Authorization API call failed");
            httpContext.Response.StatusCode = StatusCodes.Status502BadGateway;
            await httpContext.Response.WriteAsJsonAsync(
                new ProblemDetails { Status = StatusCodes.Status502BadGateway, Title = "The sign-in service is not available" },
                (System.Text.Json.JsonSerializerOptions?)null,
                "application/problem+json",
                cancellationToken);
            return true;
        }
    }
}