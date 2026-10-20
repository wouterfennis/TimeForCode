using System.Text.Json.Serialization;

namespace TimeForCode.Donation.Values
{
    /// <summary>
    /// Lifecycle state of a donation pledge. The numeric values are persisted, so existing values must not change.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum DonationStatus
    {
        Pending = 0,
        Active = 1,
        Completed = 2,
        Cancelled = 3
    }
}