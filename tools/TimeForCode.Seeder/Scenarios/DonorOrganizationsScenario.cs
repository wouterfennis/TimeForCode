using TimeForCode.Donation.Api.Client;

namespace TimeForCode.Seeder.Scenarios
{
    internal sealed class DonorOrganizationsScenario : SeedScenario
    {
        public override string Name => "donor-organizations";

        public override IReadOnlyList<Persona> Personas { get; } = [Seeder.Personas.Dana, Seeder.Personas.Dirk];

        public override async Task RunAsync(SeedContext context)
        {
            SeedClients dana = await context.ClientsFor(Seeder.Personas.Dana);
            DonorOrganizationResponse acme = await dana.Donation.CreateDonorOrganizationAsync(new CreateDonorOrganizationRequest
            {
                Name = "Acme Software",
                ContactEmail = "open-source@acme.example",
                Website = new Uri("https://acme.example")
            });
            context.Remember(SeedIds.AcmeOrganization, acme.Id);

            SeedClients dirk = await context.ClientsFor(Seeder.Personas.Dirk);
            DonorOrganizationResponse globex = await dirk.Donation.CreateDonorOrganizationAsync(new CreateDonorOrganizationRequest
            {
                Name = "Globex Engineering",
                ContactEmail = "community@globex.example",
                Website = new Uri("https://globex.example")
            });
            context.Remember(SeedIds.GlobexOrganization, globex.Id);
        }
    }
}