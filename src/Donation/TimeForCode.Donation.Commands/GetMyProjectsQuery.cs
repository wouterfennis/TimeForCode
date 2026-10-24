using MediatR;

namespace TimeForCode.Donation.Commands
{
    /// <summary>
    /// Lists the projects published by the caller in any lifecycle state, with the hours donated to each.
    /// </summary>
    public class GetMyProjectsQuery : IRequest<Result<GetMyProjectsResult>>
    {
        public required string UserId { get; init; }
    }
}