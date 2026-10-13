namespace TimeForCode.Donation.Application.Interfaces
{
    /// <summary>
    /// Normalized criteria for searching active projects. Absent criteria are <c>null</c>.
    /// </summary>
    public sealed record ProjectSearchCriteria(string? SearchText, string? Language, string? Tag, int PageNumber, int PageSize);
}