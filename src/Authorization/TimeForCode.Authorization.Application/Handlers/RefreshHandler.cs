using MediatR;
using TimeForCode.Authorization.Application.Services;
using TimeForCode.Authorization.Commands;

namespace TimeForCode.Authorization.Application.Handlers
{
    public class RefreshHandler : IRequestHandler<RefreshCommand, Result<TokenResult>>
    {
        private readonly ITokenService _tokenService;
        private readonly IRefreshTokenService _refreshTokenService;

        public RefreshHandler(ITokenService tokenService,
            IRefreshTokenService refreshTokenService)
        {
            _refreshTokenService = refreshTokenService;
            _tokenService = tokenService;
        }

        public async Task<Result<TokenResult>> Handle(RefreshCommand request, CancellationToken cancellationToken)
        {
            var internalTokenResult = await _tokenService.RefreshInternalTokenAsync(request.RefreshToken);

            if (internalTokenResult.IsFailure)
            {
                return Result<TokenResult>.Failure(internalTokenResult.ErrorMessage);
            }

            var refreshToken = await _refreshTokenService.ReplaceRefreshTokenAsync(request.RefreshToken);

            if (refreshToken.IsFailure)
            {
                return Result<TokenResult>.Failure(refreshToken.ErrorMessage);
            }

            return Result<TokenResult>.Success(new TokenResult
            {
                InternalAccessToken = internalTokenResult.Value,
                RefreshToken = refreshToken.Value
            });
        }
    }
}