namespace TimeForCode.Authorization.Infrastructure.Services.Admin
{
    /// <summary>
    /// The single conceptual "admin" user required by <c>UserManager&lt;TUser&gt;</c>/<c>IPasskeyHandler&lt;TUser&gt;</c>.
    /// There is no real user store behind this — credential storage stays in <c>AdminCredentialRepository</c>.
    /// </summary>
    public class AdminPasskeyUser
    {
        public const string AdminUserId = "admin";

        public string Id { get; init; } = AdminUserId;
    }
}
