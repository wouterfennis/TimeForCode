using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace TimeForCode.Shared.Api.Hosting
{
    /// <summary>
    /// Runs an API host with the project's default logging and error handling.
    /// </summary>
    public static class ApiHost
    {
        /// <summary>
        /// Builds and runs the host for <typeparamref name="TStartup"/>.
        /// </summary>
        /// <returns>0 if application terminates gracefully, 1 if an unexpected exception occured.</returns>
        public static int Run<TStartup>(string[] args)
            where TStartup : class
        {
            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.AddDebug();
            });

            var logger = loggerFactory.CreateLogger(nameof(ApiHost));

            try
            {
                Host.CreateDefaultBuilder(args)
                    .ConfigureLogging((_, builder) =>
                    {
                        builder.AddConsole();
                        builder.AddDebug();
                    })
                    .ConfigureWebHostDefaults(webBuilder => webBuilder.UseStartup<TStartup>())
                    .Build()
                    .Run();
                return 0;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "An unexpected exception occurred.");
                return 1;
            }
        }
    }
}