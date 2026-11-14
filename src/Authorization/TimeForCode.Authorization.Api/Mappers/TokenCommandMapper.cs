using TimeForCode.Authorization.Api.Models;
using TimeForCode.Authorization.Commands;

namespace TimeForCode.Authorization.Api.Mappers
{
    /// <summary>
    /// Maps the token requests to commands and the issued tokens to the response model.
    /// </summary>
    public static class TokenCommandMapper
    {
        /// <summary>Maps the code exchange request to its command.</summary>
        public static ExchangeCodeCommand MapToCommand(this ExchangeCodeRequestModel model)
        {
            return new ExchangeCodeCommand
            {
                Code = model.Code,
                CodeVerifier = model.CodeVerifier
            };
        }

        /// <summary>Maps the refresh request to its command.</summary>
        public static RefreshCommand MapToRefreshCommand(this RefreshRequestModel model)
        {
            return new RefreshCommand
            {
                RefreshToken = model.RefreshToken
            };
        }

        /// <summary>Maps the logout request to its command.</summary>
        public static LogoutCommand MapToLogoutCommand(this RefreshRequestModel model)
        {
            return new LogoutCommand
            {
                RefreshToken = model.RefreshToken
            };
        }

        /// <summary>Maps the issued tokens to the response model.</summary>
        public static TokenResponseModel MapToResponse(this TokenResult result)
        {
            return new TokenResponseModel
            {
                AccessToken = result.InternalAccessToken,
                RefreshToken = result.RefreshToken,
                IsNewUser = result.IsNewUser
            };
        }
    }
}