namespace TimeForCode.Authorization.Values
{
    public class RefreshToken
    {
        public required string Token { get; init; }
        public required DateTimeOffset ExpiresAfter { get; init; }

        public bool IsExpired(DateTimeOffset referenceDateTime) => referenceDateTime > ExpiresAfter;
    }
}