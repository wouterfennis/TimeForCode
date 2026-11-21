namespace TimeForCode.Shared.Identifiers
{
    /// <summary>
    /// The one place that decides whether an identifier from a route or a request can be a record identifier at all (24 hexadecimal characters).
    /// It needs no MongoDB reference, so layers that must not know the database can use it too.
    /// </summary>
    public static class ObjectIdFormat
    {
        private const int Length = 24;

        /// <summary>True when the identifier is exactly 24 hexadecimal characters.</summary>
        // Not a regular expression: its end anchor also accepts a trailing line feed, which a route value can carry.
        public static bool IsValid(string? id) => id is { Length: Length } && id.All(Uri.IsHexDigit);
    }
}