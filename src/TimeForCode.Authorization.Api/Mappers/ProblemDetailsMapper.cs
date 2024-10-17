using Microsoft.AspNetCore.Mvc;

namespace TimeForCode.Authorization.Api.Mappers
{
    /// <summary>
    /// Provides methods for mapping error messages to ProblemDetails.
    /// </summary>
    public static class ProblemDetailsMapper
    {
        /// <summary>
        /// Creates a <see cref="ProblemDetails"/> object for a bad request.
        /// </summary>
        /// <param name="errorMessage">The error message to include in the details.</param>
        /// <returns>A <see cref="ProblemDetails"/> object with status 400 and the provided error message.</returns>
        public static ProblemDetails BadRequest(string errorMessage)
        {
            return new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Bad Request",
                Detail = errorMessage
            };
        }
    }
}
