using TimeForCode.Authorization.Domain.Entities;

namespace TimeForCode.Authorization.Infrastructure.Persistence.Database
{
    /// <summary>
    /// One ASP.NET Core Data Protection key, stored so that every instance shares the same key ring.
    /// </summary>
    public class DataProtectionKeyDocument : DocumentEntity
    {
        public required string FriendlyName { get; init; }
        public required string Xml { get; init; }
    }
}