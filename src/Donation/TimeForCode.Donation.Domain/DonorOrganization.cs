namespace TimeForCode.Donation.Domain
{
    public class DonorOrganization
    {
        public string Name { get; private init; }
        public List<Contributor> EmployeeContributors { get; private init; }

        public List<Donation> Donations { get; set; }
    }
}