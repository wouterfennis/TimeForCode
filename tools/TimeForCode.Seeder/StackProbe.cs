namespace TimeForCode.Seeder
{
    /// <summary>Fails early with an actionable message when an API of the stack does not answer at all.</summary>
    internal static class StackProbe
    {
        public static async Task CheckAsync(HttpClient client, params Uri[] apis)
        {
            foreach (Uri api in apis)
            {
                try
                {
                    using HttpResponseMessage _ = await client.GetAsync(api);
                }
                catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
                {
                    throw new SeederException(
                        $"Cannot reach {api.AbsoluteUri}. Start the local stack first (scripts/start-local.ps1) or pass the right address; reason: {exception.Message}",
                        exception);
                }
            }
        }
    }
}