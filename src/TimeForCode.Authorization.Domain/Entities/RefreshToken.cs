namespace TimeForCode.Authorization.Domain.Entities
{
    public class RefreshToken : DocumentEntity
    {
        public required string Token { get; init; }
        public required DateTimeOffset ExpiresAfter { get; set; }

        public bool IsExpired(DateTimeOffset referenceDateTime) => referenceDateTime > ExpiresAfter;
        public bool SetExpiresAfter(DateTimeOffset newExpiryDateTime) => ExpiresAfter > newExpiryDateTime;
    }
}