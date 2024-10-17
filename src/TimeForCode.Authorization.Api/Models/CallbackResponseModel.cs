using System.ComponentModel.DataAnnotations;

/// <summary>
/// Represents the model for a callback response.
/// </summary>
public class CallbackResponseModel
{
    /// <summary>
    /// Gets or sets the access token.
    /// </summary>
    [Required]
    public required string AccessToken { get; init; }

    /// <summary>
    /// Creates a new instance of <see cref="CallbackResponseModel"/> with the specified access token.
    /// </summary>
    /// <param name="internalAccessToken">The internal access token to set.</param>
    /// <returns>A new instance of <see cref="CallbackResponseModel"/>.</returns>
    internal static CallbackResponseModel Create(string internalAccessToken)
    {
        return new CallbackResponseModel { AccessToken = internalAccessToken };
    }
}