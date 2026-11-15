using MediatR;

namespace TimeForCode.Donation.Commands
{
    public class GetDonorOrganizationHoursQuery : IRequest<Result<DonorOrganizationHoursResult>>
    {
        public required string DonorOrganizationId { get; init; }
    }

    public class DonorOrganizationHoursResult
    {
        public required int HoursLogged { get; init; }
    }
}