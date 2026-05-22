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

        public Hooks(IObjectContainer objectContainer)
        {
            _objectContainer = objectContainer;
        }

        [BeforeScenario]
        public void BeforeScenario()
        {
            var bearerTokenHandler = new BearerTokenHandler();
            _objectContainer.RegisterInstanceAs(bearerTokenHandler);

            var httpClient = _objectContainer.Resolve<TimeForCodeWebApplicationFactory>()
                .CreateDefaultClient(bearerTokenHandler);

            var donationClient = new DonationClient(httpClient);
            _objectContainer.RegisterInstanceAs<IDonationClient>(donationClient);

            var provider = _objectContainer.Resolve<TimeForCodeWebApplicationFactory>().Services;
            _objectContainer.RegisterInstanceAs(provider);
        }
    }
}