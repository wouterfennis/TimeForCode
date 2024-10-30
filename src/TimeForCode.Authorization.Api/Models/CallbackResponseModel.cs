using System.ComponentModel.DataAnnotations;
using TimeForCode.Authorization.Values;

/// <summary>
/// Represents the model for a callback response.
/// </summary>
public class CallbackResponseModel
{
    /// <summary>
    /// The access token.
    /// </summary>
    [Required]
    public required AccessToken AccessToken { get; init; }

    /// <summary>
    /// The access token.
    /// </summary>
    [Required]
    public required RefreshToken RefreshToken { get; init; }
}