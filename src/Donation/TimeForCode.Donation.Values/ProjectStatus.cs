using System.Text.Json.Serialization;

namespace TimeForCode.Donation.Values
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ProjectStatus
    {
        Published,
        Archived
    }
}