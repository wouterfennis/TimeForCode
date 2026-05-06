using MediatR;

namespace TimeForCode.Donation.Commands
{
    public class GetProjectsQuery : IRequest<Result<GetProjectsResult>>
    {
        public int PageNumber { get; init; } = 1;
        public int PageSize { get; init; } = 20;
    }
}
