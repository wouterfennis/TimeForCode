using System.Text.Json.Serialization;

namespace TimeForCode.Reporting.Values
{
    /// <summary>
    /// File format of the contribution history export.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ExportFormat
    {
        Json,
        Csv
    }
}