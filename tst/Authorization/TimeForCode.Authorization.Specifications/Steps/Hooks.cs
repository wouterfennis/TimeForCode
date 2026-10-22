using Microsoft.AspNetCore.Mvc.Testing.Handlers;
using Reqnroll;
using Reqnroll.BoDi;
using System.Net;
using TimeForCode.Authorization.Api.Client;
using TimeForCode.Authorization.Specifications.Mocking;

namespace TimeForCode.Authorization.Specifications.Steps
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
            var cookieContainer = new CookieContainer();
            var cookieHandler = new CookieContainerHandler(cookieContainer);
            var bearerTokenHandler = new BearerTokenFromCookieHandler(cookieContainer);

            Uri? liveAddress = LiveHost.ConfiguredBaseAddress;
            bool isLive = LiveHost.IsLive(_scenarioContext.ScenarioInfo.Tags, liveAddress);

            var httpClient = isLive
                ? LiveHost.CreateClient(liveAddress!, bearerTokenHandler, cookieHandler)
                : _objectContainer.Resolve<TimeForCodeWebApplicationFactory>()
                    .CreateDefaultClient(bearerTokenHandler, cookieHandler);

            _objectContainer.RegisterInstanceAs(cookieContainer);

            var authClient = new AuthClient(httpClient);
            _objectContainer.RegisterInstanceAs<IAuthClient>(authClient);
            _objectContainer.RegisterInstanceAs(httpClient);

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