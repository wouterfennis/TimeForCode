using Microsoft.AspNetCore.Mvc;

namespace TimeForCode.Authorization.Api.Mappers
{
    public static class ProblemDetailsMapper
    {
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
