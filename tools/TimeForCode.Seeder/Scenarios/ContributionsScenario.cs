using TimeForCode.Donation.Api.Client;

namespace TimeForCode.Seeder.Scenarios
{
    internal sealed class ContributionsScenario : SeedScenario
    {
        private const string Reference = "https://github.com/seed-owner/approved-toolkit/pull/1";

        public override string Name => "contributions";

        public override IReadOnlyList<Persona> Personas { get; } = [Seeder.Personas.Alice, Seeder.Personas.Bob];

        public override async Task RunAsync(SeedContext context)
        {
            string approved = context.IdOf(SeedIds.ApprovedProject);
            string second = context.IdOf(SeedIds.SecondApprovedProject);

            SeedClients alice = await context.ClientsFor(Seeder.Personas.Alice);
            await alice.Donation.AddContributionAsync(approved, new AddContributionRequest { Hours = 5, GithubReference = new Uri(Reference) });
            ContributionResponse overstated = await alice.Donation.AddContributionAsync(approved, new AddContributionRequest { Hours = 8 });
            await alice.Donation.CorrectContributionAsync(approved, overstated.Id, new CorrectContributionRequest { Hours = 4, GithubReference = new Uri(Reference) });

            SeedClients bob = await context.ClientsFor(Seeder.Personas.Bob);
            ContributionResponse mistaken = await bob.Donation.AddContributionAsync(approved, new AddContributionRequest { Hours = 2 });
            await bob.Donation.DeleteContributionAsync(approved, mistaken.Id);
            await bob.Donation.AddContributionAsync(second, new AddContributionRequest { Hours = 6 });
        }
    }
}