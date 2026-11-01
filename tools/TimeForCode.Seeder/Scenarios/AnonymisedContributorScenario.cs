using TimeForCode.Donation.Api.Client;

namespace TimeForCode.Seeder.Scenarios
{
    /// <summary>Carol logs hours and then deletes her account, so her contributions stay but belong to a generic deleted user.</summary>
    internal sealed class AnonymisedContributorScenario : SeedScenario
    {
        public override string Name => "anonymised-contributor";

        public override IReadOnlyList<Persona> Personas { get; } = [Seeder.Personas.Carol];

        public override async Task RunAsync(SeedContext context)
        {
            SeedClients carol = await context.ClientsFor(Seeder.Personas.Carol);
            await carol.Donation.AddContributionAsync(context.IdOf(SeedIds.ApprovedProject), new AddContributionRequest { Hours = 3 });
            await carol.Auth.DeleteAccountAsync();
        }
    }
}