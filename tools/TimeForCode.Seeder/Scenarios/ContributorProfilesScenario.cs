using TimeForCode.Donation.Api.Client;

namespace TimeForCode.Seeder.Scenarios
{
    internal sealed class ContributorProfilesScenario : SeedScenario
    {
        public override string Name => "contributor-profiles";

        public override IReadOnlyList<Persona> Personas { get; } = [Seeder.Personas.Alice, Seeder.Personas.Bob, Seeder.Personas.Dana, Seeder.Personas.Dirk];

        public override async Task RunAsync(SeedContext context)
        {
            string acme = context.IdOf(SeedIds.AcmeOrganization);
            string globex = context.IdOf(SeedIds.GlobexOrganization);

            await UpdateProfileAsync(context, Seeder.Personas.Alice, "Backend developer", ["C#", "MongoDB", "Docker"], 10, acme);
            await UpdateProfileAsync(context, Seeder.Personas.Bob, "Frontend developer", ["TypeScript", "React"], 6, acme);
            await UpdateProfileAsync(context, Seeder.Personas.Dana, "Engineering manager", ["Mentoring"], 4, acme);
            await UpdateProfileAsync(context, Seeder.Personas.Dirk, "Platform engineer", ["Kubernetes", "Bicep"], 8, globex);
        }

        private static async Task UpdateProfileAsync(SeedContext context, Persona persona, string role, string[] skills, int hoursAvailable, string organizationId)
        {
            SeedClients clients = await context.ClientsFor(persona);
            await clients.Donation.UpdateMyContributorProfileAsync(new UpdateContributorProfileRequest
            {
                Role = role,
                Skills = skills,
                HoursAvailable = hoursAvailable,
                DonorOrganizationId = organizationId
            });
        }
    }
}