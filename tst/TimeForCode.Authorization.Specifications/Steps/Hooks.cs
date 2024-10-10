using Reqnroll;
using Reqnroll.BoDi;
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
            var httpClient = _objectContainer.Resolve<TimeForCodeWebApplicationFactory>()
                .CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

            var authClient = new AuthClient(httpClient);
            _objectContainer.RegisterInstanceAs<IAuthClient>(authClient);

        }
    }
}
