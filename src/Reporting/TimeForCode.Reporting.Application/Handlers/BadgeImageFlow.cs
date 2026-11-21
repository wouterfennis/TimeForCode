using System.Net;
using TimeForCode.Reporting.Commands;

namespace TimeForCode.Reporting.Application.Handlers
{
    /// <summary>The flow the badge image handlers share: every answer is an image, so a failure becomes a state and never an error.</summary>
    internal static class BadgeImageFlow
    {
        /// <summary>Validates the identifier, sends the query and renders its value, or maps the failure to a state.</summary>
        /// <param name="isValidId">Whether the identifier is well formed; if not, the image is <paramref name="missingState"/>.</param>
        /// <param name="missingState">The state of an unknown or malformed identifier.</param>
        /// <param name="send">Sends the query.</param>
        /// <param name="render">Renders the value of a successful query.</param>
        /// <param name="logUnavailable">Logs the reason of a failure that is not "not found"; the image is then unavailable.</param>
        public static async Task<Result<BadgeImageContent>> RunAsync<T>(
            bool isValidId,
            BadgeImageState missingState,
            Func<Task<Result<T>>> send,
            Func<T, Task<BadgeImageContent>> render,
            Action<string?> logUnavailable)
        {
            if (!isValidId)
            {
                return Result<BadgeImageContent>.Success(new BadgeImageContent(missingState));
            }

            Result<T> result = await send();
            if (result.IsFailure)
            {
                if (result.FailureStatusCode is HttpStatusCode.NotFound)
                {
                    return Result<BadgeImageContent>.Success(new BadgeImageContent(missingState));
                }

                logUnavailable(result.ErrorMessage);
                return Result<BadgeImageContent>.Success(new BadgeImageContent(BadgeImageState.Unavailable));
            }

            return Result<BadgeImageContent>.Success(await render(result.Value));
        }
    }
}