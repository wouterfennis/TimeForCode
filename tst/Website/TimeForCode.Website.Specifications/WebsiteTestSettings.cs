namespace TimeForCode.Website.Specifications
{
    internal static class WebsiteTestSettings
    {
        public static string WebsiteBaseUrl =>
            Environment.GetEnvironmentVariable("WEBSITE_BASE_URL") ?? "http://localhost:8083";

        public static string IdentityProviderMockBaseUrl =>
            Environment.GetEnvironmentVariable("IDP_MOCK_BASE_URL") ?? "http://localhost:8081";
    }
}