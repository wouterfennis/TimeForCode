namespace TimeForCode.Authorization.Application.Services
{
    /// <summary>
    /// The fixed identity of the single admin session; the admin has no account record.
    /// </summary>
    public static class AdminSession
    {
        public const string UserId = "admin";
        public const string Role = "admin";
        public const string Scope = "admin";
    }
}