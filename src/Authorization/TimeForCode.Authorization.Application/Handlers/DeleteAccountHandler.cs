using MediatR;
using Microsoft.Extensions.Logging;
using System.Net;
using TimeForCode.Authorization.Application.Interfaces;
using TimeForCode.Authorization.Commands;

namespace TimeForCode.Authorization.Application.Handlers
{
    public class DeleteAccountHandler : IRequestHandler<DeleteAccountCommand, Result<Unit>>
    {
        private readonly IAccountInformationRepository _accountRepository;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IDonationService _donationService;
        private readonly ILogger<DeleteAccountHandler> _logger;

        public DeleteAccountHandler(
            IAccountInformationRepository accountRepository,
            IRefreshTokenRepository refreshTokenRepository,
            IDonationService donationService,
            ILogger<DeleteAccountHandler> logger)
        {
            _accountRepository = accountRepository;
            _refreshTokenRepository = refreshTokenRepository;
            _donationService = donationService;
            _logger = logger;
        }

        public async Task<Result<Unit>> Handle(DeleteAccountCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Deleting account {UserId}", request.UserId);

            var account = await _accountRepository.GetByInternalIdAsync(request.UserId);
            if (account == null)
            {
                _logger.LogWarning("Account {UserId} not found for deletion", request.UserId);
                return Result<Unit>.Failure("Account not found", HttpStatusCode.NotFound);
            }

            // The donation module goes first: its failure leaves the account intact so the user can retry the deletion.
            var anonymised = await _donationService.AnonymiseContributorAsync(request.UserId);
            if (anonymised.IsFailure)
            {
                _logger.LogWarning("Contributor of account {UserId} could not be anonymised: {Error}", request.UserId, anonymised.ErrorMessage);
                return Result<Unit>.Failure("The contributions of the account could not be anonymised", HttpStatusCode.BadGateway);
            }

            await _refreshTokenRepository.DeleteByUserIdAsync(request.UserId);
            await _accountRepository.DeleteByInternalIdAsync(request.UserId);

            _logger.LogInformation("Account {UserId} deleted", request.UserId);

            return Result<Unit>.Success(Unit.Value);
        }
    }
}