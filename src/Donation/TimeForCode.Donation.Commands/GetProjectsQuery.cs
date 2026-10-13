using MediatR;

namespace TimeForCode.Donation.Commands
{
    public class GetProjectsQuery : IRequest<Result<GetProjectsResult>>
    {
        public string? SearchText { get; init; }
        public string? Language { get; init; }
        public string? Tag { get; init; }
        public int PageNumber { get; init; } = 1;
        public int PageSize { get; init; } = 20;
    }
}