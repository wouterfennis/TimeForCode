namespace TimeForCode.Donation.Commands
{
    public class GetDonorOrganizationContributorsResult
    {
        public required IReadOnlyList<ContributorProfileDto> Contributors { get; init; }
    }
}