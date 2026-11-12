using Reqnroll;
using TimeForCode.Frontend.Bff.Specifications.Mocking;

namespace TimeForCode.Frontend.Bff.Specifications.Steps
{
    [Binding]
    public sealed class Hooks(ScenarioContext scenarioContext)
    {
        private static readonly Lazy<BffWebApplicationFactory> Factory = new(() => new BffWebApplicationFactory());

        [BeforeScenario(Order = 0)]
        public void SkipLiveScenarioWithoutAStack()
        {
            if (scenarioContext.ScenarioInfo.Tags.Contains(LiveHost.Tag, StringComparer.OrdinalIgnoreCase)
                && LiveHost.ConfiguredBaseAddress is null)
            {
                Assert.Inconclusive($"Needs a live stack: set {LiveHost.BaseUrlVariable} to run this @{LiveHost.Tag} scenario.");
            }
        }

        public static HttpClient CreateInProcessBrowser() => Factory.Value.CreateBrowser();

        [AfterTestRun]
        public static void DisposeFactory()
        {
            if (Factory.IsValueCreated)
            {
                Factory.Value.Dispose();
            }
        }
    }
}