using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using System.Web;
using TimeForCode.Authorization.Application.Handlers;
using TimeForCode.Authorization.Application.Interfaces;
using TimeForCode.Authorization.Application.Options;
using TimeForCode.Authorization.Commands;
using TimeForCode.Authorization.Values;

namespace TimeForCode.Authorization.Application.Tests.Handlers
{
    [TestClass]
    public class LoginHandlerTests
    {
        private LoginHandler _sut = null!;

        [TestInitialize]
        public void Setup()
        {
            var options = Microsoft.Extensions.Options.Options.Create(new ExternalIdentityProviderOptions
            {
                CallbackUri = "http://localhost/callback",
                Github = new ExternalIdentityProvider
                {
                    LoginHost = "github.example",
                    AccessTokenHost = "github.example",
                    ClientId = "client",
                    ClientSecret = "secret",
                    RestApiHost = "api.github.example",
                    MetaDataAddress = null
                }
            });
            var randomGenerator = new Mock<IRandomGenerator>();
            randomGenerator.Setup(x => x.GenerateRandomString()).Returns("state-key");

            _sut = new LoginHandler(options, Mock.Of<IStateRepository>(), randomGenerator.Object, NullLogger<LoginHandler>.Instance);
        }

        [TestMethod]
        public async Task Handle_ValidCommand_DoesNotRequestAnyScope()
        {
            var command = new LoginCommand { IdentityProvider = IdentityProvider.Github, RedirectUri = new Uri("http://localhost/") };

            var uri = await _sut.Handle(command, CancellationToken.None);

            HttpUtility.ParseQueryString(uri.Query).AllKeys.Should().NotContain("scope");
        }

        [TestMethod]
        public async Task Handle_ValidCommand_KeepsStateAndClientId()
        {
            var command = new LoginCommand { IdentityProvider = IdentityProvider.Github, RedirectUri = new Uri("http://localhost/") };

            var uri = await _sut.Handle(command, CancellationToken.None);

            var query = HttpUtility.ParseQueryString(uri.Query);
            query["state"].Should().Be("state-key");
            query["client_id"].Should().Be("client");
        }
    }
}