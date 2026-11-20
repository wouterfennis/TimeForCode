namespace TimeForCode.Seeder.Scenarios
{
    internal sealed record BadgeTierDefinition(string Name, int ThresholdHours);

    /// <summary>
    /// Defines the badge tiers once the donations and contributions exist, so the badges of the seeded data differ. The other scenarios
    /// seed these hours: contributor alice 9, bob 6 and carol 3; donor organization Acme 12 and Globex 6.
    /// </summary>
    internal sealed class BadgeTiersScenario : ISeedScenario
    {
        public string Name => "badge-tiers";

        public IReadOnlyList<Persona> Personas { get; } = [Seeder.Personas.Admin];

        /// <summary>Acme reaches Gold, Globex stays at Silver.</summary>
        public IReadOnlyList<BadgeTierDefinition> OrganizationTiers { get; } = [new("Silver", 5), new("Gold", 10)];

        /// <summary>Alice reaches Silver, Bob stays at Bronze and Carol stays below the first tier.</summary>
        public IReadOnlyList<BadgeTierDefinition> ContributorTiers { get; } = [new("Bronze", 5), new("Silver", 8)];

        public async Task RunAsync(SeedContext context)
        {
            SeedClients admin = await context.ClientsFor(Seeder.Personas.Admin);

            foreach (BadgeTierDefinition tier in OrganizationTiers)
            {
                await admin.Reporting.DefineBadgeTierAsync("organization", tier.Name, tier.ThresholdHours);
            }

            foreach (BadgeTierDefinition tier in ContributorTiers)
            {
                await admin.Reporting.DefineBadgeTierAsync("contributor", tier.Name, tier.ThresholdHours);
            }
        }
    }
}