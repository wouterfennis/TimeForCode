using System.Diagnostics.CodeAnalysis;

namespace TimeForCode.Authorization.Api
{
    /// <summary>
    /// Starting point of the Api.
    /// </summary>
    [ExcludeFromCodeCoverage(Justification = "Application entrypoint")]
    internal static class Program
    {
        /// <summary>
        /// Starting point of the Api.
        /// </summary>
        /// <returns>0 if application terminates gracefully, 1 if an unexpected exception occured.</returns>
        public static int Main(string[] args)
        {
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.AddDebug();
            });

            var logger = loggerFactory.CreateLogger(nameof(Program));

            try
            {
                CreateHostBuilder(args).Build().Run();
                return 0;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "An unexpected exception occurred.");
                return 1;
            }
            finally
            {
                if (loggerFactory is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        }

        private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureLogging((c, b) =>
            {
                b.AddConsole();
                b.AddDebug();
            })
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
            });

    }
}