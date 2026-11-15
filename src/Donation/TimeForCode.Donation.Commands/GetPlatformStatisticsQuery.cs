using MediatR;

namespace TimeForCode.Donation.Commands
{
    public class GetPlatformStatisticsQuery : IRequest<Result<PlatformStatisticsResult>>
    {
    }

    public class PlatformStatisticsResult
    {
        public required long TotalHoursLogged { get; init; }
        public required int OrganizationsSupported { get; init; }
    }
}