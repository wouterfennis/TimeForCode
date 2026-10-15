using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TimeForCode.Authorization.Application.Interfaces;
using TimeForCode.Authorization.Infrastructure.Services;

namespace TimeForCode.Authorization.Infrastructure.Tests.Services
{
    [TestClass]
    public class StoredGitHubTokenCleanupServiceTests
    {
        private Mock<IAccountInformationRepository> _repository = null!;
        private StoredGitHubTokenCleanupService _sut = null!;

        [TestInitialize]
        public void Setup()
        {
            _repository = new Mock<IAccountInformationRepository>();
            var services = new ServiceCollection();
            services.AddScoped(_ => _repository.Object);

            _sut = new StoredGitHubTokenCleanupService(
                services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>(),
                NullLogger<StoredGitHubTokenCleanupService>.Instance);
        }

        [TestMethod]
        public async Task StartAsync_RemovesTheStoredTokens()
        {
            await _sut.StartAsync(CancellationToken.None);

            _repository.Verify(x => x.RemoveStoredGitHubAccessTokensAsync(), Times.Once);
        }

        [TestMethod]
        public async Task StartAsync_RepositoryFails_DoesNotPreventStartup()
        {
            _repository.Setup(x => x.RemoveStoredGitHubAccessTokensAsync()).ThrowsAsync(new TimeoutException());

            Func<Task> act = () => _sut.StartAsync(CancellationToken.None);

            await act.Should().NotThrowAsync();
        }
    }
}