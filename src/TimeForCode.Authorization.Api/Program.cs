using TimeForCode.Authorization.Api;

internal class Program
{
    /// <summary>
    /// Starting point of the Api.
    /// </summary>
    /// <returns>0 if application terminates gracefully, 1 if an unexpected exception occured.</returns>
    public static int Main(string[] args)
    {
        try
        {
            CreateHostBuilder(args).Build().Run();
            return 0;
        }
        catch (Exception exception)
        {
            // TODO add logging
            return 1;
        }
        finally
        {
        }
    }

    private static IHostBuilder CreateHostBuilder(string[] args) =>
    Host.CreateDefaultBuilder(args)
        .ConfigureLogging((c, b) => {
            b.AddConsole();
        })
        .ConfigureWebHostDefaults(webBuilder =>
        {
            webBuilder.UseStartup<Startup>();
        });

}