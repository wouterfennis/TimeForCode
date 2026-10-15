using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using Reqnroll;
using TimeForCode.Authorization.Application.Interfaces;
using TimeForCode.Authorization.Domain.Entities;
using TimeForCode.Authorization.Infrastructure.Services;

namespace TimeForCode.Authorization.Specifications.Steps
{
    [Binding]
    internal class GithubTokenMigrationSteps
    {
        private readonly IServiceProvider _provider;
        private bool _accountHasStoredToken;

        public GithubTokenMigrationSteps(IServiceProvider provider)
        {
            _provider = provider;
        }

        [Given("An existing account has a stored access token of the external platform")]
        public void GivenAnExistingAccountHasAStoredAccessTokenOfTheExternalPlatform()
        {
            _accountHasStoredToken = true;

            var repository = _provider.GetRequiredService<Mock<IAccountInformationRepository>>();
            repository.Setup(x => x.RemoveStoredGitHubAccessTokensAsync())
                .Callback(() => _accountHasStoredToken = false)
                .ReturnsAsync(1L);
        }

        [When("The time for code platform migrates its stored accounts")]
        public async Task WhenTheTimeForCodePlatformMigratesItsStoredAccountsAsync()
        {
            var migration = _provider.GetServices<IHostedService>().OfType<StoredGitHubTokenCleanupService>().Single();

            await migration.StartAsync(CancellationToken.None);
        }

        [Then("No access token of the external platform is stored for the account")]
        public void ThenNoAccessTokenOfTheExternalPlatformIsStoredForTheAccount()
        {
            _accountHasStoredToken.Should().BeFalse();
        }

        [Then("The account profile is unchanged")]
        public void ThenTheAccountProfileIsUnchanged()
        {
            var repository = _provider.GetRequiredService<Mock<IAccountInformationRepository>>();

            repository.Verify(x => x.CreateOrUpdateAsync(It.IsAny<AccountInformation>()), Times.Never);
        }
    }
}