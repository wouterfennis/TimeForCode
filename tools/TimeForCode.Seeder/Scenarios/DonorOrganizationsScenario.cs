using TimeForCode.Donation.Api.Client;

namespace TimeForCode.Seeder.Scenarios
{
    internal sealed class DonorOrganizationsScenario : ISeedScenario
    {
        public string Name => "donor-organizations";

        public IReadOnlyList<Persona> Personas { get; } = [Seeder.Personas.Dana, Seeder.Personas.Dirk];

        public async Task RunAsync(SeedContext context)
        {
            SeedClients dana = await context.ClientsFor(Seeder.Personas.Dana);
            DonorOrganizationResponse acme = await dana.Donation.CreateDonorOrganizationAsync(new CreateDonorOrganizationRequest
            {
                Name = "Acme Software",
                ContactEmail = "open-source@acme.example",
                Website = ExampleSite("acme.example")
            });
            context.Remember(SeedIds.AcmeOrganization, acme.Id);

            SeedClients dirk = await context.ClientsFor(Seeder.Personas.Dirk);
            DonorOrganizationResponse globex = await dirk.Donation.CreateDonorOrganizationAsync(new CreateDonorOrganizationRequest
            {
                Name = "Globex Engineering",
                ContactEmail = "community@globex.example",
                Website = ExampleSite("globex.example")
            });
            context.Remember(SeedIds.GlobexOrganization, globex.Id);
        }

        private static Uri ExampleSite(string host) => new UriBuilder(Uri.UriSchemeHttps, host).Uri;
    }
}