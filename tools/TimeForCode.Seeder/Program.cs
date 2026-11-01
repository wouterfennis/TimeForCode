using TimeForCode.Authorization.Api.Client;

namespace TimeForCode.Seeder
{
    internal static class Program
    {
        private static async Task<int> Main(string[] args)
        {
            try
            {
                SeederOptions options = SeederOptions.Resolve(args, Environment.GetEnvironmentVariable);

                using var probeClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
                await StackProbe.CheckAsync(probeClient, options.AuthorizationUrl, options.DonationUrl);

                await new SeedRunner(new LiveSeedSession(options), Console.WriteLine).RunAsync(ScenarioCatalog.All);

                Console.WriteLine("Seeding finished.");
                return 0;
            }
            catch (SeederException exception)
            {
                Console.Error.WriteLine($"Seeding failed: {exception.Message}");
                return 1;
            }
            catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or ApiException)
            {
                Console.Error.WriteLine($"Seeding failed: {exception.Message}");
                return 1;
            }
        }
    }
}