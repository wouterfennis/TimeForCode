using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Reqnroll;
using System.Net;
using System.Text.Json;
using System.Web;
using TimeForCode.Authorization.Api.Client;
using TimeForCode.Authorization.Api.Client.Extensions;
using TimeForCode.Authorization.Api.Controllers;
using TimeForCode.Authorization.Application.Interfaces;
using TimeForCode.Authorization.Application.Interfaces.Admin;
using TimeForCode.Authorization.Application.Services;
using TimeForCode.Authorization.Domain.Entities;
using TimeForCode.Authorization.Specifications.TestBuilder;
using TimeForCode.Shared.Api.Authentication;

namespace TimeForCode.Authorization.Specifications.Steps
{
    [Binding]
    internal class AdminAuthenticationSteps
    {
        private readonly IAuthClient _authClient;
        private readonly IServiceProvider _provider;
        private readonly CookieContainer _cookieContainer;
        private TryResponse<CallbackResponseModel?, ApiException?>? _result;

        public AdminAuthenticationSteps(IAuthClient authClient, IServiceProvider provider, CookieContainer cookieContainer)
        {
            _authClient = authClient;
            _provider = provider;
            _cookieContainer = cookieContainer;
        }

        [When("The user logs in as admin with the registered device")]
        public async Task WhenTheUserLogsInAsAdminWithTheRegisteredDeviceAsync()
        {
            SeedAssertionStateCookie();
            _result = await _authClient.TryAdminCompleteAuthenticationAsync("credential-json");
        }

        [When("The user logs in as admin with a device that is not the registered device")]
        public async Task WhenTheUserLogsInAsAdminWithADeviceThatIsNotTheRegisteredDeviceAsync()
        {
            var ceremonyService = _provider.GetRequiredService<Mock<IPasskeyCeremonyService>>();
            ceremonyService.Setup(x => x.CompleteAuthenticationAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(AdminAssertionOutcomeBuilder.BuildFailed());

            SeedAssertionStateCookie();
            _result = await _authClient.TryAdminCompleteAuthenticationAsync("credential-json-from-unregistered-device");
        }

        private void SeedAssertionStateCookie()
        {
            var uri = new Uri("http://localhost:8083");
            _cookieContainer.Add(uri, new Cookie(AdminAuthenticationController.AssertionStateCookieKey, "protected-ceremony-state"));
        }

        [Given("The user has an admin session token")]
        public void GivenTheUserHasAnAdminSessionToken()
        {
            using var scope = _provider.CreateScope();
            var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
            var accessToken = tokenService.GenerateInternalToken("admin", "admin", "admin");

            var uri = new Uri("http://localhost:8083");
            var cookieValue = HttpUtility.UrlEncode(JsonSerializer.Serialize(accessToken));
            _cookieContainer.Add(uri, new Cookie(CookieConstants.TokenKey, cookieValue));
        }

        [Then("The admin login succeeds")]
        public void ThenTheAdminLoginSucceeds()
        {
            _result!.Exception.Should().BeNull();
            _result!.Response.Should().NotBeNull();
        }

        [Then("An admin session token is issued containing the admin role and the admin scope")]
        public void ThenAnAdminSessionTokenIsIssuedContainingTheAdminRoleAndTheAdminScope()
        {
            _result!.Response.Should().NotBeNull();
            _result!.Response!.AccessToken.Should().NotBeNull();

            var jwt = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler()
                .ReadJwtToken(_result!.Response!.AccessToken.Token);

            jwt.Claims.Should().Contain(c => c.Type == "role" && c.Value == "admin");
            jwt.Claims.Should().Contain(c => c.Type == "scope" && c.Value == "admin");
        }

        [Then("The admin login is rejected")]
        public void ThenTheAdminLoginIsRejected()
        {
            _result!.Exception.Should().NotBeNull();
        }

        [Then("No admin session token is issued")]
        public void ThenNoAdminSessionTokenIsIssued()
        {
            _result!.Response.Should().BeNull();
        }

        [Then("The admin session token is no longer valid")]
        public void ThenTheAdminSessionTokenIsNoLongerValid()
        {
            var repository = _provider.GetRequiredService<Mock<IRefreshTokenRepository>>();
            repository.Verify(x => x.DeleteAsync(It.IsAny<Domain.Entities.RefreshToken>()), Times.Once);
        }
    }
}
