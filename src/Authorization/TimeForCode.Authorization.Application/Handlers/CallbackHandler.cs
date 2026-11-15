using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TimeForCode.Authorization.Application.Interfaces;
using TimeForCode.Authorization.Application.Options;
using TimeForCode.Authorization.Application.Services;
using TimeForCode.Authorization.Commands;
using TimeForCode.Authorization.Domain.Entities;

namespace TimeForCode.Authorization.Application.Handlers
{
    /// <summary>
    /// Completes the sign-in at the identity provider and hands the client a single-use code. The code is
    /// exchanged for tokens later (<see cref="ExchangeCodeHandler"/>), so no token travels through the browser.
    /// </summary>
    public class CallbackHandler : IRequestHandler<CallbackCommand, Result<CallbackResult>>
    {
        private readonly IStateRepository _stateRepository;
        private readonly ILoginCodeRepository _loginCodeRepository;
        private readonly IRandomGenerator _randomGenerator;
        private readonly AuthenticationOptions _authenticationOptions;
        private readonly TimeProvider _timeProvider;
        private readonly IAccountService _accountService;
        private readonly ITokenService _tokenService;
        private readonly ILogger<CallbackHandler> _logger;

        public CallbackHandler(IStateRepository stateRepository,
            ILoginCodeRepository loginCodeRepository,
            IRandomGenerator randomGenerator,
            IOptions<AuthenticationOptions> authenticationOptions,
            TimeProvider timeProvider,
            IAccountService accountService,
            ITokenService tokenService,
            ILogger<CallbackHandler> logger)
        {
            _stateRepository = stateRepository;
            _loginCodeRepository = loginCodeRepository;
            _randomGenerator = randomGenerator;
            _authenticationOptions = authenticationOptions.Value;
            _timeProvider = timeProvider;
            _accountService = accountService;
            _tokenService = tokenService;
            _logger = logger;
        }

        public async Task<Result<CallbackResult>> Handle(CallbackCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Handling callback");

            var now = _timeProvider.GetUtcNow();
            var stateEntry = await _stateRepository.TakeAsync(request.State);
            if (stateEntry == null)
            {
                _logger.LogWarning("Callback received with an unknown or already used state");
                return Result<CallbackResult>.Failure("State is not known");
            }

            if (stateEntry.IsExpired(now))
            {
                _logger.LogWarning("Callback received with an expired state");
                return Result<CallbackResult>.Failure("State has expired");
            }

            var accessTokenResult = await _tokenService.GetAccessTokenFromExternalProviderAsync(stateEntry, request.Code);
            if (accessTokenResult.IsFailure)
            {
                _logger.LogWarning("Failed to get access token from external provider: {Error}", accessTokenResult.ErrorMessage);
                return Result<CallbackResult>.Failure(accessTokenResult.ErrorMessage);
            }

            var saveResult = await _accountService.SaveAccountInformation(stateEntry.IdentityProvider, accessTokenResult.Value);

            if (saveResult.IsFailure)
            {
                _logger.LogWarning("Failed to save account information: {Error}", saveResult.ErrorMessage);
                return Result<CallbackResult>.Failure(saveResult.ErrorMessage);
            }

            var userId = saveResult.Value.AccountInformation.Id.ToString();
            var code = _randomGenerator.GenerateRandomString();
            await _loginCodeRepository.AddAsync(LoginCode.Issue(
                RefreshTokenHasher.Hash(code),
                userId,
                saveResult.Value.IsNewAccount,
                stateEntry.ClientCodeChallenge,
                now.AddSeconds(_authenticationOptions.LoginCodeLifetimeInSeconds)));

            _logger.LogInformation("Callback handled successfully for user {UserId}", userId);

            return Result<CallbackResult>.Success(new CallbackResult
            {
                RedirectUri = stateEntry.RedirectUri,
                Code = code
            });
        }
    }
}