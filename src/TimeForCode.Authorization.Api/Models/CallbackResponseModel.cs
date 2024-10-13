using System.ComponentModel.DataAnnotations;

public class CallbackResponseModel
{
    [Required]
    public required string AccessToken { get; init; }

    internal static CallbackResponseModel Create(string internalAccessToken)
    {
        return new CallbackResponseModel { AccessToken = internalAccessToken };
    }
}