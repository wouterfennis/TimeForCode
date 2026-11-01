using Microsoft.Extensions.DependencyInjection;
using Reqnroll;
using Reqnroll.BoDi;
using TimeForCode.Reporting.Specifications.Mocking;

namespace TimeForCode.Reporting.Specifications.Steps
{
    [Binding]
    public class Hooks
    {
        private static readonly Lazy<TimeForCodeWebApplicationFactory> SharedHost = new(() => new TimeForCodeWebApplicationFactory());

        private readonly IObjectContainer _objectContainer;

        private readonly ScenarioContext _scenarioContext;

        public Hooks(IObjectContainer objectContainer, ScenarioContext scenarioContext)
        {
            _objectContainer = objectContainer;
            _scenarioContext = scenarioContext;
        }

        /// <summary>Runs after the clients are registered, so the AfterScenario hooks of a skipped scenario can still resolve their dependencies.</summary>
        [BeforeScenario(Order = 1)]
        public void SkipWhenLiveStackIsRequired()
        {
            if (LiveHost.RequiresLiveHost(_scenarioContext.ScenarioInfo.Tags, LiveHost.ConfiguredBaseAddress))
            {
                Assert.Inconclusive($"Needs a live stack: set {LiveHost.BaseUrlVariable} to run this @{LiveHost.LiveOnlyTag} scenario.");
            }
        }

        [BeforeScenario(Order = 0)]
        public void BeforeScenario()
        {
            Uri? liveAddress = LiveHost.ConfiguredBaseAddress;

            var bearerTokenHandler = new BearerTokenHandler();
            _objectContainer.RegisterInstanceAs(bearerTokenHandler);

            bool isLive = LiveHost.IsLive(_scenarioContext.ScenarioInfo.Tags, liveAddress);
            _objectContainer.RegisterInstanceAs(new LiveScenario(isLive));

            if (isLive)
            {
                _objectContainer.RegisterInstanceAs(LiveHost.CreateClient(liveAddress!, bearerTokenHandler));

                // A live stack holds its own data; the in-memory fake only absorbs the arrange steps that have no live meaning.
                _objectContainer.RegisterInstanceAs(new InMemoryDonationDataSource());
                return;
            }

            // Starting the host is the expensive part of a scenario, so every in-process scenario reuses one host that is reset first.
            TimeForCodeWebApplicationFactory factory = SharedHost.Value;
            factory.ResetState();

            _objectContainer.RegisterInstanceAs(factory.CreateDefaultClient(bearerTokenHandler));
            _objectContainer.RegisterInstanceAs(factory.Services.GetRequiredService<InMemoryDonationDataSource>());
        }

        [AfterTestRun]
        public static void DisposeSharedFactory()
        {
            if (SharedHost.IsValueCreated)
            {
                SharedHost.Value.Dispose();
            }
        }

        // Hooks without an order run at 10000, so this must be higher to dispose the client after the clean-up hooks used it.
        [AfterScenario(Order = 20000)]
        public void DisposeLiveClient()
        {
            if (_objectContainer.IsRegistered<LiveScenario>()
                && _objectContainer.Resolve<LiveScenario>().IsLive
                && _objectContainer.IsRegistered<HttpClient>())
            {
                _objectContainer.Resolve<HttpClient>().Dispose();
            }
        }
    }
}