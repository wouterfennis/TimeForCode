namespace TimeForCode.Donation.Api.V1.Models
{
    /// <summary>
    /// Represents an administrator's request for changes to a project under review.
    /// </summary>
    public class RequestProjectChangesRequest
    {
        /// <summary>
        /// The reason the changes are requested. Shown to the maintainer.
        /// </summary>
        public required string Reason { get; init; }
    }
}