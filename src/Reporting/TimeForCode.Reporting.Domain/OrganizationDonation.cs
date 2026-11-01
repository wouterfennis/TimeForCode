using TimeForCode.Reporting.Values;

namespace TimeForCode.Reporting.Domain
{
    /// <summary>
    /// A donation pledged for a donor organization, as read from the Donation context.
    /// </summary>
    public record OrganizationDonation(string ProjectId, DonationStatus Status, int HoursCommitted, int HoursLogged);
}