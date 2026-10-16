using TimeForCode.Authorization.Values;

namespace TimeForCode.Authorization.Application.Services
{
    /// <summary>
    /// The outcome of a successful refresh token rotation: the new token and the user it belongs to.
    /// </summary>
    public record RefreshTokenReplacement(RefreshToken RefreshToken, string UserId);
}