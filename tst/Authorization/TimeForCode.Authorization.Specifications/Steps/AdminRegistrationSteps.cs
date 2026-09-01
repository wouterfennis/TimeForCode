using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Reqnroll;
using System.Net;
using TimeForCode.Authorization.Api.Client;
using TimeForCode.Authorization.Api.Client.Extensions;
using TimeForCode.Authorization.Api.Controllers;
using TimeForCode.Authorization.Application.Interfaces.Admin;
using TimeForCode.Authorization.Domain.Entities;
using TimeForCode.Authorization.Specifications.TestBuilder;

namespace TimeForCode.Authorization.Specifications.Steps
{
    [Binding]
    internal class AdminRegistrationSteps
    {
        private readonly IAuthClient _authClient;
        private readonly IServiceProvider _provider;
        private readonly CookieContainer _cookieContainer;
        private TryResponse<CallbackResponseModel?, ApiException?>? _firstResult;
        private TryResponse<CallbackResponseModel?, ApiException?>? _secondResult;

        public AdminRegistrationSteps(IAuthClient authClient, IServiceProvider provider, CookieContainer cookieContainer)
        {
            _authClient = authClient;
            _provider = provider;
            _cookieContainer = cookieContainer;
        }

        private void SeedAttestationStateCookie()
        {
            var uri = new Uri("http://localhost:8083");
            _cookieContainer.Add(uri, new Cookie(AdminAuthenticationController.AttestationStateCookieKey, "protected-ceremony-state"));
        }

        [Given("The time for code platform has no registered admin credential")]
        public void GivenTheTimeForCodePlatformHasNoRegisteredAdminCredential()
        {
            var repository = _provider.GetRequiredService<Mock<IAdminCredentialRepository>>();
            repository.Setup(x => x.GetAsync()).ReturnsAsync((AdminCredential?)null);
        }

        [Given("The time for code platform already has a registered admin credential")]
        public void GivenTheTimeForCodePlatformAlreadyHasARegisteredAdminCredential()
        {
            var repository = _provider.GetRequiredService<Mock<IAdminCredentialRepository>>();
            repository.Setup(x => x.GetAsync()).ReturnsAsync(AdminCredentialBuilder.Build());
            repository.Setup(x => x.TryClaimAsync(It.IsAny<AdminCredential>())).ReturnsAsync(false);
        }

        [When("The user registers an admin passkey with the correct bootstrap secret")]
        public async Task WhenTheUserRegistersAnAdminPasskeyWithTheCorrectBootstrapSecretAsync()
        {
            SeedAttestationStateCookie();
            _firstResult = await _authClient.TryAdminCompleteRegistrationAsync(Constants.AdminBootstrapSecret, "credential-json");
        }

        [When("The user registers an admin passkey with an incorrect bootstrap secret")]
        public async Task WhenTheUserRegistersAnAdminPasskeyWithAnIncorrectBootstrapSecretAsync()
        {
            SeedAttestationStateCookie();
            _firstResult = await _authClient.TryAdminCompleteRegistrationAsync("wrong-secret", "credential-json");
        }

        [When("Two devices simultaneously register an admin passkey with the correct bootstrap secret")]
        public async Task WhenTwoDevicesSimultaneouslyRegisterAnAdminPasskeyWithTheCorrectBootstrapSecretAsync()
        {
            SeedAttestationStateCookie();

            var repository = _provider.GetRequiredService<Mock<IAdminCredentialRepository>>();
            var claimCount = 0;
            repository.Setup(x => x.TryClaimAsync(It.IsAny<AdminCredential>()))
                .ReturnsAsync(() => Interlocked.Increment(ref claimCount) == 1);

            var firstTask = _authClient.TryAdminCompleteRegistrationAsync(Constants.AdminBootstrapSecret, "credential-json-1");
            var secondTask = _authClient.TryAdminCompleteRegistrationAsync(Constants.AdminBootstrapSecret, "credential-json-2");

            await Task.WhenAll(firstTask, secondTask);
            _firstResult = firstTask.Result;
            _secondResult = secondTask.Result;
        }

        [Then("The admin passkey registration succeeds")]
        public void ThenTheAdminPasskeyRegistrationSucceeds()
        {
            _firstResult!.Exception.Should().BeNull();
            _firstResult!.Response.Should().NotBeNull();
        }

        [Then("The admin passkey registration is rejected")]
        public void ThenTheAdminPasskeyRegistrationIsRejected()
        {
            _firstResult!.Exception.Should().NotBeNull();
        }

        [Then("The time for code platform permanently claims the admin role for the user's device")]
        public void ThenTheTimeForCodePlatformPermanentlyClaimsTheAdminRoleForTheUsersDevice()
        {
            _firstResult!.Response.Should().NotBeNull();
            _firstResult!.Response!.AccessToken.Should().NotBeNull();
        }

        [Then("The time for code platform still has no registered admin credential")]
        public void ThenTheTimeForCodePlatformStillHasNoRegisteredAdminCredential()
        {
            var repository = _provider.GetRequiredService<Mock<IAdminCredentialRepository>>();
            repository.Verify(x => x.TryClaimAsync(It.IsAny<AdminCredential>()), Times.Never);
        }

        [Then("The time for code platform still has only the original registered admin credential")]
        public void ThenTheTimeForCodePlatformStillHasOnlyTheOriginalRegisteredAdminCredential()
        {
            _firstResult!.Exception.Should().NotBeNull();
        }

        [Then("Only one of the two admin passkey registrations succeeds")]
        public void ThenOnlyOneOfTheTwoAdminPasskeyRegistrationsSucceeds()
        {
            var results = new[] { _firstResult!, _secondResult! };
            results.Count(r => r.Exception == null).Should().Be(1);
            results.Count(r => r.Exception != null).Should().Be(1);
        }

        [Then("The time for code platform has exactly one registered admin credential")]
        public void ThenTheTimeForCodePlatformHasExactlyOneRegisteredAdminCredential()
        {
            var repository = _provider.GetRequiredService<Mock<IAdminCredentialRepository>>();
            repository.Verify(x => x.TryClaimAsync(It.IsAny<AdminCredential>()), Times.Exactly(2));
        }
    }
}