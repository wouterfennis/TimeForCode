namespace TimeForCode.Reporting.Application.Handlers
{
    /// <summary>The one place that decides whether an identifier from a public route can be a record identifier at all.</summary>
    internal static class ObjectIdFormat
    {
        // Not a regular expression: its end anchor also accepts a trailing line feed, which a route value can carry.
        public static bool IsValid(string id) => id.Length == 24 && id.All(Uri.IsHexDigit);
    }
}