namespace TimeForCode.Authorization.Domain.Entities
{
    public enum RefreshTokenReplacementCheck
    {
        Allowed,
        Expired,
        Replayed
    }
}