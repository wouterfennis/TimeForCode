using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Reqnroll;
using RichardSzalay.MockHttp;
using System.Net;
using TimeForCode.Authorization.Api.Client;
using TimeForCode.Authorization.Api.Client.Extensions;
using TimeForCode.Authorization.Application.Interfaces;
using TimeForCode.Authorization.Domain.Entities;
using TimeForCode.Authorization.Specifications.Mocking;
using TimeForCode.Authorization.Specifications.TestBuilder;
using TimeForCode.Shared.Api.Authentication;
using IServiceProvider = System.IServiceProvider;

namespace TimeForCode.Authorization.Specifications.Steps
{
    [Binding]
    internal class DeleteAccountSteps
    {
        private const string OtherUserId = "000000000000000000000002";
        private const string AnonymiseUrl = "*/api/v1/contributor/*/anonymise";

        private readonly IAuthClient _authClient;
        private readonly IServiceProvider _provider;
        private readonly CookieContainer _cookieContainer;
        private readonly SessionContext _session;
        private readonly ScenarioInfrastructure _infrastructure;
        private readonly LogoutSteps _logoutSteps;
        private readonly HashSet<string> _profiles = [];
        private MockedRequest? _anonymiseRequest;
        private bool _donationCannotAnonymise;
        private bool _repositoriesConfigured;
        private TryVoid<ApiException?>? _result;

        public DeleteAccountSteps(
            IAuthClient authClient,
            IServiceProvider provider,
            CookieContainer cookieContainer,
            SessionContext session,
            ScenarioInfrastructure infrastructure,
            LogoutSteps logoutSteps)
        {
            _authClient = authClient;
            _provider = provider;
            _cookieContainer = cookieContainer;
            _session = session;
            _infrastructure = infrastructure;
            _logoutSteps = logoutSteps;
        }

        [Given("The user has a profile on the time for code platform")]
        public void GivenTheUserHasAProfileOnTheTimeForCodePlatform()
        {
            ConfigureRepositories();
            _profiles.Add(_session.UserId);
        }

        [Given("Another user has a profile on the time for code platform")]
        public void GivenAnotherUserHasAProfileOnTheTimeForCodePlatform()
        {
            ConfigureRepositories();
            _profiles.Add(OtherUserId);
        }

        [Given("The donation module cannot anonymise the contributor of the user")]
        public void GivenTheDonationModuleCannotAnonymiseTheContributorOfTheUser()
        {
            _donationCannotAnonymise = true;
        }

        [Given("The user has deleted their account")]
        public async Task GivenTheUserHasDeletedTheirAccountAsync()
        {
            if (_cookieContainer.GetAllCookies()[CookieConstants.TokenKey] == null)
            {
                _logoutSteps.GivenTheUserHasAnAccessToken();
            }

            // The deletion expires the cookies of the client; copies of the tokens that the user kept must stay unusable.
            var keptTokens = _cookieContainer.GetAllCookies().ToList();

            await DeleteAsync();

            _result!.Exception.Should().BeNull();
            foreach (var kept in keptTokens)
            {
                _cookieContainer.Add(new Uri("http://localhost:8083"), new Cookie(kept.Name, kept.Value));
            }
        }

        [When("The user deletes their account")]
        public async Task WhenTheUserDeletesTheirAccountAsync()
        {
            await DeleteAsync();
        }

        [Then("The account deletion is confirmed")]
        public void ThenTheAccountDeletionIsConfirmed()
        {
            _result.Should().NotBeNull();
            _result!.Exception.Should().BeNull();
        }

        [Then("The profile of the user is removed")]
        public void ThenTheProfileOfTheUserIsRemoved()
        {
            _profiles.Should().NotContain(_session.UserId);
        }

        [Then("The profile of the user is kept")]
        public void ThenTheProfileOfTheUserIsKept()
        {
            _profiles.Should().Contain(_session.UserId);
        }

        [Then("The profile of the other user is kept")]
        public void ThenTheProfileOfTheOtherUserIsKept()
        {
            _profiles.Should().Contain(OtherUserId);
        }

        [Then("The donation module is asked to anonymise the contributor of the user")]
        public void ThenTheDonationModuleIsAskedToAnonymiseTheContributorOfTheUser()
        {
            _anonymiseRequest.Should().NotBeNull();
            _infrastructure.MockHttp.GetMatchCount(_anonymiseRequest!).Should().Be(1);
        }

        [Then("The user is informed the account could not be deleted")]
        public void ThenTheUserIsInformedTheAccountCouldNotBeDeleted()
        {
            _session.LastException.Should().NotBeNull();
            _session.LastException!.StatusCode.Should().Be(StatusCodes.Status502BadGateway);
        }

        [Then("The user is informed the account deletion requires authentication")]
        public void ThenTheUserIsInformedTheAccountDeletionRequiresAuthentication()
        {
            _session.LastException.Should().NotBeNull();
            _session.LastException!.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        }

        private async Task DeleteAsync()
        {
            ConfigureDonationModule();

            _result = await _authClient.TryDeleteAccountAsync();
            _session.LastException = _result.Exception;
        }

        private void ConfigureDonationModule()
        {
            if (_anonymiseRequest != null)
            {
                return;
            }

            var request = _infrastructure.MockHttp.When(HttpMethod.Post, AnonymiseUrl);
            if (_donationCannotAnonymise)
            {
                _anonymiseRequest = request.Respond(HttpStatusCode.InternalServerError);
                return;
            }

            _anonymiseRequest = request.Respond(HttpStatusCode.NoContent);
        }

        private void ConfigureRepositories()
        {
            if (_repositoriesConfigured)
            {
                return;
            }

            _repositoriesConfigured = true;

            var accounts = _provider.GetRequiredService<Mock<IAccountInformationRepository>>();
            accounts.Setup(x => x.GetByInternalIdAsync(It.IsAny<string>()))
                .ReturnsAsync((string id) => _profiles.Contains(id) ? AccountInformationBuilder.Build() : null!);
            accounts.Setup(x => x.DeleteByInternalIdAsync(It.IsAny<string>()))
                .Callback((string id) => _profiles.Remove(id))
                .Returns(Task.CompletedTask);

            // Once the refresh tokens of the user are deleted, none of them can be found any more
            var refreshTokens = _provider.GetRequiredService<Mock<IRefreshTokenRepository>>();
            refreshTokens.Setup(x => x.DeleteByUserIdAsync(It.IsAny<string>()))
                .Callback(() => refreshTokens.Setup(x => x.GetByTokenHashAsync(It.IsAny<string>()))
                    .ReturnsAsync((Domain.Entities.RefreshToken?)null))
                .Returns(Task.CompletedTask);
        }
    }
}