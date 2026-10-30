namespace TimeForCode.Reporting.Api.V1
{
    /// <summary>
    /// Limits and patterns of the API contract.
    /// </summary>
    internal static class ApiConstraints
    {
        /// <summary>A 24-character hexadecimal object identifier, the shape of the identifiers the Donation context issues.</summary>
        internal const string ObjectIdPattern = "^[0-9a-fA-F]{24}$";
    }
}