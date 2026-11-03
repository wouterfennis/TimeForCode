using AwesomeAssertions;
using TimeForCode.Seeder;
using TimeForCode.Seeder.Scenarios;

namespace TimeForCode.Seeder.Tests
{
    [TestClass]
    public class ScenarioTests
    {
        [TestMethod]
        public async Task ProjectLifecycle_ApprovedProject_IsSubmittedByTheMaintainerThenApprovedByTheAdmin()
        {
            var stack = new FakeStack();
            SeedContext context = await RunProjectLifecycleAsync(stack);

            string id = context.IdOf(SeedIds.ApprovedProject);
            stack.Calls.IndexOf($"maya-maintainer:Submit:{id}").Should().BeGreaterThanOrEqualTo(0)
                .And.BeLessThan(stack.Calls.IndexOf($"admin:Approve:{id}"));
        }

        [TestMethod]
        public async Task ProjectLifecycle_DraftProject_IsNeverSubmitted()
        {
            var stack = new FakeStack();
            SeedContext context = await RunProjectLifecycleAsync(stack);

            stack.Calls.Should().NotContain($"maya-maintainer:Submit:{context.IdOf(SeedIds.DraftProject)}");
        }

        [TestMethod]
        public async Task ProjectLifecycle_PendingProject_IsSubmittedButNotDecided()
        {
            var stack = new FakeStack();
            SeedContext context = await RunProjectLifecycleAsync(stack);

            string id = context.IdOf(SeedIds.PendingProject);
            stack.Calls.Should().Contain($"maya-maintainer:Submit:{id}");
            stack.Calls.Should().NotContain(c => c.StartsWith("admin:") && c.Contains(id));
        }

        [TestMethod]
        public async Task ProjectLifecycle_ChangesRequestedProject_GetsAReasonFromTheAdmin()
        {
            var stack = new FakeStack();
            SeedContext context = await RunProjectLifecycleAsync(stack);

            stack.Calls.Should().Contain(c => c.StartsWith($"admin:RequestChanges:{context.IdOf(SeedIds.ChangesRequestedProject)}:") && c.Length > $"admin:RequestChanges:{context.IdOf(SeedIds.ChangesRequestedProject)}:".Length);
        }

        [TestMethod]
        public async Task ProjectLifecycle_ArchivedProject_IsApprovedThenArchivedByTheMaintainer()
        {
            var stack = new FakeStack();
            SeedContext context = await RunProjectLifecycleAsync(stack);

            string id = context.IdOf(SeedIds.ArchivedProject);
            stack.Calls.IndexOf($"admin:Approve:{id}").Should().BeGreaterThanOrEqualTo(0)
                .And.BeLessThan(stack.Calls.IndexOf($"maya-maintainer:Archive:{id}"));
        }

        [TestMethod]
        public async Task ProjectLifecycle_RegistersEveryProjectOnce()
        {
            var stack = new FakeStack();
            await RunProjectLifecycleAsync(stack);

            stack.Calls.Where(c => c.Contains(":RegisterProject:")).Should().OnlyHaveUniqueItems().And.HaveCount(6);
        }

        [TestMethod]
        public async Task Contributions_AContributionIsCorrectedAndAnotherIsDeleted()
        {
            var stack = new FakeStack();
            await RunUpToAsync(stack, "contributions");

            stack.Calls.Should().Contain(c => c.Contains(":CorrectContribution:"));
            stack.Calls.Should().Contain(c => c.Contains(":DeleteContribution:"));
        }

        [TestMethod]
        public async Task Donations_OrganizationAndIndividualDonationsAreActivated()
        {
            var stack = new FakeStack();
            await RunUpToAsync(stack, "donations");

            stack.Calls.Count(c => c.Contains(":Pledge:")).Should().BeGreaterThanOrEqualTo(3);
            stack.Calls.Should().Contain(c => c.Contains(":Activate:"));
            stack.Calls.Should().Contain(c => c.Contains(":CancelDonation:"));
        }

        [TestMethod]
        public async Task AnonymisedContributor_LogsHoursBeforeDeletingTheAccount()
        {
            var stack = new FakeStack();
            await RunUpToAsync(stack, "anonymised-contributor");

            int logged = stack.Calls.FindIndex(c => c.StartsWith("carol-contributor:AddContribution:"));
            int deleted = stack.Calls.IndexOf("carol-contributor:DeleteAccount");
            logged.Should().BeGreaterThanOrEqualTo(0);
            deleted.Should().BeGreaterThan(logged);
        }

        [TestMethod]
        public async Task ContributorProfiles_ContributorsJoinTheDonorOrganization()
        {
            var stack = new FakeStack();
            await RunUpToAsync(stack, "contributor-profiles");

            stack.Calls.Should().Contain(c => c.StartsWith("alice-contributor:UpdateProfile:organization-"));
        }

        private static async Task<SeedContext> RunProjectLifecycleAsync(FakeStack stack)
        {
            var ids = new Dictionary<string, string>();
            var scenario = new ProjectLifecycleScenario();
            var context = new SeedContext(scenario.Name, scenario.Personas, stack, ids);
            await scenario.RunAsync(context);
            return context;
        }

        private static async Task RunUpToAsync(FakeStack stack, string lastScenario)
        {
            int last = ScenarioCatalog.All.ToList().FindIndex(s => s.Name == lastScenario);
            await stack.RunAsync(ScenarioCatalog.All.Take(last + 1).ToArray());
        }
    }
}