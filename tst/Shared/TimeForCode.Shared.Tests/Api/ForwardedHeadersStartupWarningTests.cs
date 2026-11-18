using AwesomeAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TimeForCode.Shared.Api.Extensions;

namespace TimeForCode.Shared.Tests.Api
{
    /// <summary>
    /// Without trusted proxies only loopback may set the forwarded headers, so behind a real proxy every client shares the proxy's address
    /// (one rate-limit bucket for everybody). The host says so at start instead of failing silently.
    /// </summary>
    [TestClass]
    public class ForwardedHeadersStartupWarningTests
    {
        public TestContext TestContext { get; set; } = null!;

        private async Task<List<string>> WarningsAtStartAsync(string environment, Dictionary<string, string?> settings)
        {
            var warnings = new List<string>();
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
            using var host = await new HostBuilder()
                .UseEnvironment(environment)
                .ConfigureLogging(logging => logging.ClearProviders().AddProvider(new WarningCollector(warnings)))
                .ConfigureWebHost(web => web
                    .UseTestServer()
                    .ConfigureServices(services => services.AddTrustedForwardedHeaders(configuration))
                    .Configure(_ => { }))
                .StartAsync(TestContext.CancellationToken);
            return warnings;
        }

        [TestMethod]
        public async Task Start_ProductionWithoutTrustedProxies_LogsAWarning()
        {
            var warnings = await WarningsAtStartAsync(Environments.Production, []);

            warnings.Should().ContainSingle().Which.Should().Contain("ForwardedHeaders");
        }

        [TestMethod]
        public async Task Start_ProductionWithAKnownProxy_LogsNoWarning()
        {
            var warnings = await WarningsAtStartAsync(Environments.Production, new() { ["ForwardedHeaders:KnownProxies:0"] = "10.0.0.5" });

            warnings.Should().BeEmpty();
        }

        [TestMethod]
        public async Task Start_DevelopmentWithoutTrustedProxies_LogsNoWarning()
        {
            var warnings = await WarningsAtStartAsync(Environments.Development, []);

            warnings.Should().BeEmpty();
        }

        private sealed class WarningCollector(List<string> warnings) : ILoggerProvider
        {
            public ILogger CreateLogger(string categoryName) => new Collector(warnings);

            public void Dispose()
            {
            }

            private sealed class Collector(List<string> warnings) : ILogger
            {
                public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

                public bool IsEnabled(LogLevel logLevel) => true;

                public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
                {
                    if (logLevel >= LogLevel.Warning)
                    {
                        warnings.Add(formatter(state, exception));
                    }
                }
            }
        }
    }
}