using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TimeForCode.Authorization.Application.Interfaces;

namespace TimeForCode.Authorization.Infrastructure.Services
{
    /// <summary>
    /// Removes the GitHub access tokens that earlier versions stored on accounts. Idempotent, so it runs on every start.
    /// </summary>
    public class StoredGitHubTokenCleanupService : IHostedService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<StoredGitHubTokenCleanupService> _logger;

        public StoredGitHubTokenCleanupService(IServiceScopeFactory scopeFactory, ILogger<StoredGitHubTokenCleanupService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var repository = scope.ServiceProvider.GetRequiredService<IAccountInformationRepository>();

                var removed = await repository.RemoveStoredGitHubAccessTokensAsync();

                _logger.LogInformation("Removed the stored GitHub access token from {Count} accounts", removed);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Removing the stored GitHub access tokens failed; it is retried on the next start");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}