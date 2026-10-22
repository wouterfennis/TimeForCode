namespace TimeForCode.Donation.Specifications.Mocking
{
    /// <summary>
    /// Switches a scenario tagged <c>@live</c> from the in-process factory to a running stack.
    /// </summary>
    internal static class LiveHost
    {
        public const string BaseUrlVariable = "TIMEFORCODE_LIVE_DONATION_URL";
        public const string Tag = "live";

        /// <summary>Marks a <c>@live</c> scenario that needs real persistence and is skipped by the in-process run.</summary>
        public const string LiveOnlyTag = "liveonly";

        public static IServiceProvider UnavailableServices { get; } = new UnavailableServiceProvider();

        public static Uri? ConfiguredBaseAddress =>
            ResolveBaseAddress(Environment.GetEnvironmentVariable(BaseUrlVariable));

        public static Uri RequiredBaseAddress =>
            ConfiguredBaseAddress ?? throw new InvalidOperationException($"{BaseUrlVariable} must be set for live steps.");

        public static Uri? ResolveBaseAddress(string? configured)
        {
            if (string.IsNullOrWhiteSpace(configured))
            {
                return null;
            }

            if (!Uri.TryCreate(configured, UriKind.Absolute, out Uri? baseAddress)
                || (baseAddress.Scheme != Uri.UriSchemeHttp && baseAddress.Scheme != Uri.UriSchemeHttps))
            {
                throw new InvalidOperationException($"{BaseUrlVariable} must be an absolute http(s) URL, but was '{configured}'.");
            }

            return baseAddress;
        }

        public static bool IsLive(IEnumerable<string> scenarioTags, Uri? baseAddress)
        {
            return baseAddress is not null && scenarioTags.Contains(Tag, StringComparer.OrdinalIgnoreCase);
        }

        public static bool RequiresLiveHost(IEnumerable<string> scenarioTags, Uri? baseAddress)
        {
            IReadOnlyCollection<string> tags = scenarioTags.ToList();

            return tags.Contains(LiveOnlyTag, StringComparer.OrdinalIgnoreCase) && !IsLive(tags, baseAddress);
        }

        public static HttpClient CreateClient(Uri baseAddress, params DelegatingHandler[] handlers)
        {
            HttpMessageHandler pipeline = new HttpClientHandler { UseCookies = false };

            foreach (DelegatingHandler handler in handlers.Reverse())
            {
                handler.InnerHandler = pipeline;
                pipeline = handler;
            }

            return new HttpClient(pipeline) { BaseAddress = baseAddress };
        }

        private sealed class UnavailableServiceProvider : IServiceProvider
        {
            public object? GetService(Type serviceType)
            {
                throw new NotSupportedException(
                    $"The in-process service '{serviceType.Name}' does not exist on a live host; this scenario cannot be tagged @live.");
            }
        }
    }
}