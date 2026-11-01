using AwesomeAssertions;
using Moq;
using TimeForCode.Authorization.Api.Client;
using TimeForCode.Donation.Api.Client;
using TimeForCode.Seeder;

namespace TimeForCode.Seeder.Tests
{
    [TestClass]
    public class SeedRunnerTests
    {
        private static readonly Persona Alice = new("alice-contributor", PersonaRole.Contributor);
        private static readonly Persona Maya = new("maya-maintainer", PersonaRole.Maintainer);

        [TestMethod]
        public async Task RunAsync_Scenarios_RunInTheGivenOrder()
        {
            var ran = new List<string>();
            var runner = new SeedRunner(new RecordingSession(), _ => { });

            await runner.RunAsync([
                new DelegateScenario("first", [], _ => { ran.Add("first"); return Task.CompletedTask; }),
                new DelegateScenario("second", [], _ => { ran.Add("second"); return Task.CompletedTask; })]);

            ran.Should().Equal("first", "second");
        }

        [TestMethod]
        public async Task RunAsync_ScenarioSignsInAsDeclaredPersona_UsesTheSession()
        {
            var session = new RecordingSession();
            var runner = new SeedRunner(session, _ => { });

            await runner.RunAsync([new DelegateScenario("s", [Alice], ctx => ctx.ClientsFor(Alice))]);

            session.SignedInAs.Should().Equal(Alice.Name);
        }

        [TestMethod]
        public async Task RunAsync_ScenarioSignsInAsUndeclaredPersona_Throws()
        {
            var runner = new SeedRunner(new RecordingSession(), _ => { });

            Func<Task> act = () => runner.RunAsync([new DelegateScenario("s", [Alice], ctx => ctx.ClientsFor(Maya))]);

            await act.Should().ThrowAsync<SeederException>().WithMessage("*s*maya-maintainer*");
        }

        [TestMethod]
        public async Task RunAsync_IdRememberedByAScenario_IsVisibleToTheNextScenario()
        {
            string? seen = null;
            var runner = new SeedRunner(new RecordingSession(), _ => { });

            await runner.RunAsync([
                new DelegateScenario("first", [], ctx => { ctx.Remember("project", "p1"); return Task.CompletedTask; }),
                new DelegateScenario("second", [], ctx => { seen = ctx.IdOf("project"); return Task.CompletedTask; })]);

            seen.Should().Be("p1");
        }

        [TestMethod]
        public async Task RunAsync_ScenarioFails_StopsAndNamesTheScenario()
        {
            var ran = new List<string>();
            var runner = new SeedRunner(new RecordingSession(), _ => { });

            Func<Task> act = () => runner.RunAsync([
                new DelegateScenario("broken", [], _ => throw new InvalidOperationException("boom")),
                new DelegateScenario("after", [], _ => { ran.Add("after"); return Task.CompletedTask; })]);

            await act.Should().ThrowAsync<SeederException>().WithMessage("*broken*boom*");
            ran.Should().BeEmpty();
        }

        [TestMethod]
        public async Task RunAsync_Scenarios_AreLoggedByName()
        {
            var log = new List<string>();
            var runner = new SeedRunner(new RecordingSession(), log.Add);

            await runner.RunAsync([new DelegateScenario("projects", [], _ => Task.CompletedTask)]);

            log.Should().ContainSingle(l => l.Contains("projects"));
        }

        internal sealed class RecordingSession : ISeedSession
        {
            public List<string> SignedInAs { get; } = [];

            public Task<SeedClients> SignInAsync(Persona persona)
            {
                SignedInAs.Add(persona.Name);
                return Task.FromResult(new SeedClients(new Mock<IDonationClient>().Object, new Mock<IAuthClient>().Object));
            }
        }

        internal sealed class DelegateScenario : SeedScenario
        {
            private readonly Func<SeedContext, Task> _run;

            public DelegateScenario(string name, IReadOnlyList<Persona> personas, Func<SeedContext, Task> run)
            {
                Name = name;
                Personas = personas;
                _run = run;
            }

            public override string Name { get; }

            public override IReadOnlyList<Persona> Personas { get; }

            public override Task RunAsync(SeedContext context) => _run(context);
        }
    }
}