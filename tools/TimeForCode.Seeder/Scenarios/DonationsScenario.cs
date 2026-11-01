using TimeForCode.Donation.Api.Client;

namespace TimeForCode.Seeder.Scenarios
{
    internal sealed class DonationsScenario : SeedScenario
    {
        public override string Name => "donations";

        public override IReadOnlyList<Persona> Personas { get; } = [Seeder.Personas.Dana, Seeder.Personas.Dirk, Seeder.Personas.Alice];

        public override async Task RunAsync(SeedContext context)
        {
            string approved = context.IdOf(SeedIds.ApprovedProject);
            string second = context.IdOf(SeedIds.SecondApprovedProject);

            SeedClients dana = await context.ClientsFor(Seeder.Personas.Dana);
            DonationResponse acme = await dana.Donation.PledgeDonationAsync(approved, new PledgeDonationRequest
            {
                Hours = 40,
                DonorOrganizationId = context.IdOf(SeedIds.AcmeOrganization)
            });
            await dana.Donation.ActivateDonationAsync(acme.Id);

            SeedClients dirk = await context.ClientsFor(Seeder.Personas.Dirk);
            DonationResponse globex = await dirk.Donation.PledgeDonationAsync(second, new PledgeDonationRequest
            {
                Hours = 25,
                DonorOrganizationId = context.IdOf(SeedIds.GlobexOrganization)
            });
            await dirk.Donation.ActivateDonationAsync(globex.Id);

            DonationResponse cancelled = await dirk.Donation.PledgeDonationAsync(approved, new PledgeDonationRequest
            {
                Hours = 15,
                DonorOrganizationId = context.IdOf(SeedIds.GlobexOrganization)
            });
            await dirk.Donation.CancelDonationAsync(cancelled.Id);

            SeedClients alice = await context.ClientsFor(Seeder.Personas.Alice);
            await alice.Donation.PledgeDonationAsync(second, new PledgeDonationRequest
            {
                Hours = 5,
                AsIndividual = true
            });
        }
    }
}