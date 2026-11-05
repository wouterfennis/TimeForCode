using System.Text.Json.Serialization;

namespace TimeForCode.Reporting.Values
{
    /// <summary>
    /// File format of the contribution history export.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ExportFormat
    {
        /// <summary>A JSON document with a <c>contributions</c> list.</summary>
        Json,
        /// <summary>A CSV file with one row per contribution.</summary>
        Csv
    }
}