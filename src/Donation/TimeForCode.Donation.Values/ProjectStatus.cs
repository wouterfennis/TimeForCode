using System.Text.Json.Serialization;

namespace TimeForCode.Donation.Values
{
    /// <summary>
    /// Lifecycle state of a project. The numeric values are persisted, so existing values must not change:
    /// <c>Active</c> and <c>Archived</c> keep the values of the former <c>Published</c> and <c>Archived</c> states.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ProjectStatus
    {
        Active = 0,
        Archived = 1,
        Draft = 2,
        PendingApproval = 3
    }
}