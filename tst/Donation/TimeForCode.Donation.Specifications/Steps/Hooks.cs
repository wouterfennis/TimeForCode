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

        [BeforeScenario]
        public void BeforeScenario()
        {
            var bearerTokenHandler = new BearerTokenHandler();
            _objectContainer.RegisterInstanceAs(bearerTokenHandler);

            Uri? liveAddress = LiveHost.ConfiguredBaseAddress;
            bool isLive = LiveHost.IsLive(_scenarioContext.ScenarioInfo.Tags, liveAddress);

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
    }
}