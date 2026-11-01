using TimeForCode.Seeder.Scenarios;

namespace TimeForCode.Seeder
{
    /// <summary>The scenarios in the order they run; later ones rely on ids that earlier ones remember.</summary>
    internal static class ScenarioCatalog
    {
        public static IReadOnlyList<SeedScenario> All { get; } =
        [
            new DonorOrganizationsScenario(),
            new ContributorProfilesScenario(),
            new ProjectLifecycleScenario(),
            new DonationsScenario(),
            new ContributionsScenario(),
            new AnonymisedContributorScenario()
        ];
    }
}