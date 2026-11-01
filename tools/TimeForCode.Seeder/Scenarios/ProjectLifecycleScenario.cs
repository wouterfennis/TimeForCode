using TimeForCode.Donation.Api.Client;

namespace TimeForCode.Seeder.Scenarios
{
    /// <summary>
    /// One project per reachable lifecycle state. The API has no rejection, so the administrator's decisions are
    /// approval and returning a project with a reason; <c>Archived</c> is the closest to a rejected project.
    /// </summary>
    internal sealed class ProjectLifecycleScenario : SeedScenario
    {
        private const string RepositoryOwner = "https://github.com/seed-owner/";

        public override string Name => "project-lifecycle";

        public override IReadOnlyList<Persona> Personas { get; } = [Seeder.Personas.Maya, Seeder.Personas.Marco, Seeder.Personas.Admin];

        public override async Task RunAsync(SeedContext context)
        {
            SeedClients maya = await context.ClientsFor(Seeder.Personas.Maya);
            SeedClients marco = await context.ClientsFor(Seeder.Personas.Marco);
            SeedClients admin = await context.ClientsFor(Seeder.Personas.Admin);

            await RegisterAsync(context, maya, SeedIds.DraftProject, "draft-notes");

            string pending = await RegisterAsync(context, maya, SeedIds.PendingProject, "pending-parser");
            await maya.Donation.SubmitProjectForReviewAsync(pending);

            string approved = await RegisterAsync(context, maya, SeedIds.ApprovedProject, "approved-toolkit");
            await maya.Donation.SubmitProjectForReviewAsync(approved);
            await admin.Donation.ApproveProjectAsync(approved);

            string changes = await RegisterAsync(context, maya, SeedIds.ChangesRequestedProject, "needs-work-cli");
            await maya.Donation.SubmitProjectForReviewAsync(changes);
            await admin.Donation.RequestProjectChangesAsync(changes, new RequestProjectChangesRequest
            {
                Reason = "Please add a README with build instructions and a licence file."
            });

            string archived = await RegisterAsync(context, maya, SeedIds.ArchivedProject, "archived-legacy");
            await maya.Donation.SubmitProjectForReviewAsync(archived);
            await admin.Donation.ApproveProjectAsync(archived);
            await maya.Donation.ArchiveProjectAsync(archived);

            string second = await RegisterAsync(context, marco, SeedIds.SecondApprovedProject, "second-approved-library");
            await marco.Donation.SubmitProjectForReviewAsync(second);
            await admin.Donation.ApproveProjectAsync(second);
        }

        private static async Task<string> RegisterAsync(SeedContext context, SeedClients maintainer, string key, string repository)
        {
            RegisterProjectResult result = await maintainer.Donation.RegisterProjectAsync(new RegisterProjectRequest
            {
                GithubRepositoryUrl = new Uri(RepositoryOwner + repository)
            });
            context.Remember(key, result.ProjectId);
            return result.ProjectId;
        }
    }
}