using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using TimeForCode.Authorization.Api;

namespace TimeForCode.Authorization.Specifications.Mocking
{
    /// <summary>
    /// A time for code platform with its own environment and configuration, built straight from <see cref="Startup"/>
    /// so that a failing startup surfaces its exception instead of being swallowed by the entry point.
    /// </summary>
    internal class ConfiguredPlatformFactory : TimeForCodeWebApplicationFactory
    {
        private readonly string _environment;
        private readonly IReadOnlyDictionary<string, string?> _settings;

        public ConfiguredPlatformFactory(ScenarioInfrastructure infrastructure, string environment, IReadOnlyDictionary<string, string?> settings)
            : base(infrastructure)
        {
            _environment = environment;
            _settings = settings;
        }

        protected override IHostBuilder CreateHostBuilder() =>
            Host.CreateDefaultBuilder().ConfigureWebHostDefaults(webBuilder => webBuilder.UseStartup<Startup>());

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.UseEnvironment(_environment);
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(_settings));
        }
    }
}