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
            await _sut.ExecuteTask!;

            _repository.Verify(x => x.RemoveStoredGitHubAccessTokensAsync(), Times.Once);
        }

        [TestMethod]
        public async Task StartAsync_RepositoryFails_DoesNotPreventStartup()
        {
            _repository.Setup(x => x.RemoveStoredGitHubAccessTokensAsync()).ThrowsAsync(new TimeoutException());

            Func<Task> act = async () =>
            {
                await _sut.StartAsync(CancellationToken.None);
                await _sut.ExecuteTask!;
            };

            await act.Should().NotThrowAsync();
        }

        [TestMethod]
        public async Task StartAsync_SlowRepository_DoesNotBlockHostStartup()
        {
            var neverCompletes = new TaskCompletionSource<long>();
            _repository.Setup(x => x.RemoveStoredGitHubAccessTokensAsync()).Returns(neverCompletes.Task);

            var start = _sut.StartAsync(CancellationToken.None);

            await start.WaitAsync(TimeSpan.FromSeconds(5));
            start.IsCompletedSuccessfully.Should().BeTrue();
            neverCompletes.SetResult(0);
            await _sut.StopAsync(CancellationToken.None);
        }
    }
}