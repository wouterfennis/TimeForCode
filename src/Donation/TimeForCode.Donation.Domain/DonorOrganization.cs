namespace TimeForCode.Donation.Domain
{
    public class DonorOrganization
    {
        public required string Name { get; init; }
        public required List<Contributor> EmployeeContributors { get; init; }

        public List<Donation> Donations { get; set; } = [];
    }
}