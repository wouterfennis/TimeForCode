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

        public Hooks(IObjectContainer objectContainer)
        {
            _objectContainer = objectContainer;
        }

        [BeforeScenario]
        public void BeforeScenario()
        {
            var cookieContainer = new CookieContainer();
            var cookieHandler = new CookieContainerHandler(cookieContainer);

            var httpClient = _objectContainer.Resolve<TimeForCodeWebApplicationFactory>()
                .CreateDefaultClient(cookieHandler);

            _objectContainer.RegisterInstanceAs(cookieContainer);

            var authClient = new AuthClient(httpClient);
            _objectContainer.RegisterInstanceAs<IAuthClient>(authClient);

            var provider = _objectContainer.Resolve<TimeForCodeWebApplicationFactory>().Services;
            _objectContainer.RegisterInstanceAs(provider);
        }
    }
}