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

        public Hooks(IObjectContainer objectContainer)
        {
            _objectContainer = objectContainer;
        }

        [BeforeScenario]
        public void BeforeScenario()
        {
            var bearerTokenHandler = new BearerTokenHandler();
            _objectContainer.RegisterInstanceAs(bearerTokenHandler);

            // Starting the host is the expensive part of a scenario, so every scenario reuses one host that is reset first.
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
    }
}