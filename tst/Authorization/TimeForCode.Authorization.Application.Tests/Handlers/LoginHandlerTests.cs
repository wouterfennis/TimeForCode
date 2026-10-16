using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using TimeForCode.Authorization.Application.Handlers;
using TimeForCode.Authorization.Application.Interfaces;
using TimeForCode.Authorization.Application.Options;
using TimeForCode.Authorization.Commands;
using TimeForCode.Authorization.Domain.Entities;
using TimeForCode.Authorization.Values;

namespace TimeForCode.Authorization.Application.Tests.Handlers
{
    [TestClass]
    public class LoginHandlerTests
    {
        private const string Verifier = "verifier-0123456789-0123456789-0123456789-abc";
        private static readonly DateTimeOffset Now = new(2026, 10, 16, 12, 0, 0, TimeSpan.Zero);
        private Mock<IStateRepository> _stateRepository = null!;
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
            randomGenerator.Setup(x => x.GenerateCodeVerifier()).Returns(Verifier);
            _stateRepository = new Mock<IStateRepository>();
            var timeProvider = new Mock<TimeProvider>();
            timeProvider.Setup(x => x.GetUtcNow()).Returns(Now);

            _sut = new LoginHandler(options, _stateRepository.Object, randomGenerator.Object, timeProvider.Object, NullLogger<LoginHandler>.Instance);
        }

        private static LoginCommand Command() => new() { IdentityProvider = IdentityProvider.Github, RedirectUri = new Uri("http://localhost/") };

        [TestMethod]
        public async Task Handle_ValidCommand_DoesNotRequestAnyScope()
        {
            var uri = await _sut.Handle(Command(), CancellationToken.None);

            HttpUtility.ParseQueryString(uri.Query).AllKeys.Should().NotContain("scope");
        }

        [TestMethod]
        public async Task Handle_ValidCommand_KeepsStateAndClientId()
        {
            var uri = await _sut.Handle(Command(), CancellationToken.None);

            var query = HttpUtility.ParseQueryString(uri.Query);
            query["state"].Should().Be("state-key");
            query["client_id"].Should().Be("client");
        }

        [TestMethod]
        public async Task Handle_ValidCommand_SendsS256ChallengeOfTheStoredVerifier()
        {
            var uri = await _sut.Handle(Command(), CancellationToken.None);

            var query = HttpUtility.ParseQueryString(uri.Query);
            query["code_challenge_method"].Should().Be("S256");
            query["code_challenge"].Should().Be(Convert.ToBase64String(SHA256.HashData(Encoding.ASCII.GetBytes(Verifier)))
                .TrimEnd('=').Replace('+', '-').Replace('/', '_'));
        }

        [TestMethod]
        public async Task Handle_ValidCommand_StoresStateWithVerifierAndTenMinuteExpiry()
        {
            StateEntry? stored = null;
            _stateRepository.Setup(x => x.AddAsync(It.IsAny<StateEntry>()))
                .Callback<StateEntry>(e => stored = e)
                .Returns(Task.CompletedTask);

            await _sut.Handle(Command(), CancellationToken.None);

            stored.Should().NotBeNull();
            stored!.Key.Should().Be("state-key");
            stored.CodeVerifier.Should().Be(Verifier);
            stored.ExpiresAfter.Should().Be(Now.AddMinutes(10));
        }
    }
}