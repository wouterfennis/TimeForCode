using System.Text.Json.Serialization;

namespace TimeForCode.Reporting.Values
{
    /// <summary>
    /// Lifecycle state of a donation as the Donation context reports it; duplicated here on purpose (ADR-010).
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum DonationStatus
    {
        /// <summary>Not yet committed; does not count towards reported hours.</summary>
        Pending,
        /// <summary>Committed and in progress; counts towards reported hours.</summary>
        Active,
        /// <summary>Fulfilled; counts towards reported hours.</summary>
        Completed,
        /// <summary>No longer committed; does not count towards reported hours.</summary>
        Cancelled
    }
}