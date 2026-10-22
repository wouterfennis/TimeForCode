using Reqnroll;
using Reqnroll.BoDi;
using TimeForCode.Donation.Api.Client;
using TimeForCode.Donation.Specifications.Mocking;

namespace TimeForCode.Donation.Specifications.Steps
{
    [Binding]
    public class Hooks
    {
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

            var httpClient = isLive
                ? LiveHost.CreateClient(liveAddress!, bearerTokenHandler)
                : _objectContainer.Resolve<TimeForCodeWebApplicationFactory>()
                    .CreateDefaultClient(bearerTokenHandler);

            _objectContainer.RegisterInstanceAs(httpClient);

            var donationClient = new DonationClient(httpClient);
            _objectContainer.RegisterInstanceAs<IDonationClient>(donationClient);

            if (isLive)
            {
                _objectContainer.RegisterInstanceAs(LiveHost.UnavailableServices);
                return;
            }

            var provider = _objectContainer.Resolve<TimeForCodeWebApplicationFactory>().Services;
            _objectContainer.RegisterInstanceAs(provider);
        }

        [AfterScenario(Order = 100)]
        public void DisposeLiveClient()
        {
            if (_objectContainer.IsRegistered<LiveScenario>() && _objectContainer.Resolve<LiveScenario>().IsLive)
            {
                _objectContainer.Resolve<HttpClient>().Dispose();
            }
        }
    }
}