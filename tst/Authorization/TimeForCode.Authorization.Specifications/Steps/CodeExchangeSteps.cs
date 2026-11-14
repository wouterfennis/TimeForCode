using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Reqnroll;
using System.IdentityModel.Tokens.Jwt;
using TimeForCode.Authorization.Api.Client;
using TimeForCode.Authorization.Api.Client.Extensions;
using TimeForCode.Authorization.Application.Interfaces;
using TimeForCode.Authorization.Application.Services;
using TimeForCode.Authorization.Specifications.Mocking;
using IServiceProvider = System.IServiceProvider;

namespace TimeForCode.Authorization.Specifications.Steps
{
    [Binding]
    internal class CodeExchangeSteps
    {
        private readonly IAuthClient _authClient;
        private readonly IServiceProvider _provider;
        private readonly ScenarioInfrastructure _infrastructure;
        private readonly SessionContext _session;

        public CodeExchangeSteps(IAuthClient authClient, IServiceProvider provider, ScenarioInfrastructure infrastructure, SessionContext session)
        {
            _authClient = authClient;
            _provider = provider;
            _infrastructure = infrastructure;
            _session = session;
        }

        [Given("The user has completed the sign-in")]
        public void GivenTheUserHasCompletedTheSignIn()
        {
            _session.OneTimeCode.Should().NotBeNullOrEmpty("the sign-in has to be completed first");
        }

        [Given("The one-time code has already been used")]
        public async Task GivenTheOneTimeCodeHasAlreadyBeenUsedAsync()
        {
            await ExchangeAsync(_session.OneTimeCode!, Constants.ClientCodeVerifier);
            _session.LastException.Should().BeNull();
        }

        [Given("More than a minute has passed since the sign-in was completed")]
        public void GivenMoreThanAMinuteHasPassedSinceTheSignInWasCompleted()
        {
            _infrastructure.Time.Advance(TimeSpan.FromSeconds(61));
        }

        [Given("The user exchanges the one-time code")]
        [When("The user exchanges the one-time code")]
        public async Task WhenTheUserExchangesTheOneTimeCodeAsync()
        {
            await ExchangeAsync(_session.OneTimeCode!, Constants.ClientCodeVerifier);
        }

        [Given("The user exchanges the one-time code with a verifier of another login")]
        [When("The user exchanges the one-time code with a verifier of another login")]
        public async Task WhenTheUserExchangesTheOneTimeCodeWithAVerifierOfAnotherLoginAsync()
        {
            await ExchangeAsync(_session.OneTimeCode!, "other-verifier-0123456789-0123456789-0123456789");
        }

        [When("The user exchanges a one-time code that was never issued")]
        public async Task WhenTheUserExchangesAOneTimeCodeThatWasNeverIssuedAsync()
        {
            await ExchangeAsync("never-issued", Constants.ClientCodeVerifier);
        }

        [When("The user exchanges the one-time code with a verifier that is too short")]
        public async Task WhenTheUserExchangesTheOneTimeCodeWithAVerifierThatIsTooShortAsync()
        {
            await ExchangeAsync(_session.OneTimeCode!, "short");
        }

        [Then("An access token is returned")]
        public void ThenAnAccessTokenIsReturned()
        {
            _session.Tokens.Should().NotBeNull();
            _session.Tokens!.AccessToken.Token.Should().NotBeNullOrEmpty();
        }

        [Then("A refresh token is returned")]
        public void ThenARefreshTokenIsReturned()
        {
            _session.Tokens.Should().NotBeNull();
            _session.Tokens!.RefreshToken.Token.Should().NotBeNullOrEmpty();
        }

        [Then("The access token contains the user scope")]
        public void ThenTheAccessTokenContainsTheUserScope()
        {
            ReadAccessToken().Claims.Should().Contain(c => c.Type == "scope" && c.Value == "user");
        }

        [Then("The access token contains the admin role and the admin scope")]
        public void ThenTheAccessTokenContainsTheAdminRoleAndTheAdminScope()
        {
            var jwt = ReadAccessToken();

            jwt.Claims.Should().Contain(c => c.Type == "role" && c.Value == "admin");
            jwt.Claims.Should().Contain(c => c.Type == "scope" && c.Value == "admin");
        }

        [Then("The time for code platform notifies the user that their profile has been created")]
        public void ThenTheTimeForCodePlatformNotifiesTheUserThatTheirProfileHasBeenCreated()
        {
            _session.Tokens!.IsNewUser.Should().BeTrue();
        }

        [Then("The time for code platform does not notify the user of a new profile")]
        public void ThenTheTimeForCodePlatformDoesNotNotifyTheUserOfANewProfile()
        {
            _session.Tokens!.IsNewUser.Should().BeFalse();
        }

        [Then("The refresh token is not stored in readable form")]
        public void ThenTheRefreshTokenIsNotStoredInReadableForm()
        {
            var repository = _provider.GetRequiredService<Mock<IRefreshTokenRepository>>();
            var returned = _session.Tokens!.RefreshToken.Token;

            repository.Verify(x => x.CreateAsync(It.Is<Domain.Entities.RefreshToken>(t => t.TokenHash == RefreshTokenHasher.Hash(returned))), Times.Once);
            repository.Verify(x => x.CreateAsync(It.Is<Domain.Entities.RefreshToken>(t => t.TokenHash == returned)), Times.Never);
        }

        [Then("The exchange is refused")]
        public void ThenTheExchangeIsRefused()
        {
            _session.LastException.Should().NotBeNull();
            _session.Tokens.Should().BeNull();
        }

        private async Task ExchangeAsync(string code, string verifier)
        {
            var result = await _authClient.TryExchangeCodeAsync(code, verifier);
            _session.LastException = result.Exception;
            _session.Tokens = result.Response;
        }

        private JwtSecurityToken ReadAccessToken()
        {
            _session.Tokens.Should().NotBeNull();
            return new JwtSecurityTokenHandler().ReadJwtToken(_session.Tokens!.AccessToken.Token);
        }
    }
}