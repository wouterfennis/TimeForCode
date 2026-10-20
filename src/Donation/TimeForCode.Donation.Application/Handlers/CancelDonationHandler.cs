using MediatR;
using Microsoft.Extensions.Logging;
using TimeForCode.Donation.Application.Exceptions;
using TimeForCode.Donation.Application.Interfaces;
using TimeForCode.Donation.Commands;

namespace TimeForCode.Donation.Application.Handlers
{
    internal class CancelDonationHandler : IRequestHandler<CancelDonationCommand, Result<DonationResult>>
    {
        private readonly IDonationRepository _donationRepository;
        private readonly ILogger<CancelDonationHandler> _logger;

        public CancelDonationHandler(IDonationRepository donationRepository, ILogger<CancelDonationHandler> logger)
        {
            _donationRepository = donationRepository;
            _logger = logger;
        }

        public async Task<Result<DonationResult>> Handle(CancelDonationCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Cancelling donation {DonationId} for user {UserId}", request.DonationId, request.UserId);

            var donation = await _donationRepository.GetByIdAsync(request.DonationId);
            if (donation == null)
            {
                _logger.LogWarning("Donation {DonationId} not found", request.DonationId);
                return Result<DonationResult>.NotFound("Donation not found.");
            }

            if (!donation.CanBeChangedBy(request.UserId, request.IsAdministrator))
            {
                _logger.LogWarning("User {UserId} may not change donation {DonationId}", request.UserId, request.DonationId);
                return Result<DonationResult>.Forbidden("Only the donor or an administrator can change a donation.");
            }

            var transition = donation.Cancel();
            if (!transition.IsSuccess)
            {
                return Result<DonationResult>.Conflict(transition.ErrorMessage);
            }

            try
            {
                await _donationRepository.UpdateAsync(donation);
            }
            catch (RepositoryConflictException ex)
            {
                _logger.LogWarning(ex, "Donation {DonationId} was changed concurrently", request.DonationId);
                return Result<DonationResult>.Conflict("The donation was changed by someone else; reload it and try again.");
            }

            return Result<DonationResult>.Success(new DonationResult { Donation = donation.ToDto() });
        }
    }
}