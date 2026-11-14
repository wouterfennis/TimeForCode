using MediatR;

namespace TimeForCode.Donation.Commands
{
    public class GetContributorHoursQuery : IRequest<Result<ContributorHoursResult>>
    {
        public required string ContributorId { get; init; }
    }

    public class ContributorHoursResult
    {
        public required int TotalHours { get; init; }
    }
}