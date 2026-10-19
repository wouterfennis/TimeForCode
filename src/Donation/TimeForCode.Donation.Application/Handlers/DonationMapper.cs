using TimeForCode.Donation.Commands;

namespace TimeForCode.Donation.Application.Handlers
{
    internal static class DonationMapper
    {
        internal static DonationDto ToDto(this Domain.Donation donation)
        {
            return new DonationDto
            {
                Id = donation.Id.ToString(),
                ProjectId = donation.ProjectId.ToString(),
                DonorOrganizationId = donation.DonorOrganizationId?.ToString(),
                Status = donation.Status.ToString(),
                HoursCommitted = donation.HoursCommitted,
                HoursLogged = donation.HoursLogged,
                ProgressPercentage = donation.ProgressPercentage,
                CreatedAt = donation.CreatedAt,
                UpdatedAt = donation.UpdatedAt
            };
        }
    }
}