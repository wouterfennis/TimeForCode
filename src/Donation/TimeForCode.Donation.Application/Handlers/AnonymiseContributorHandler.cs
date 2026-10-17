using MediatR;
using Microsoft.Extensions.Logging;
using TimeForCode.Donation.Application.Interfaces;
using TimeForCode.Donation.Commands;

namespace TimeForCode.Donation.Application.Handlers
{
    internal class AnonymiseContributorHandler : IRequestHandler<AnonymiseContributorCommand, Result>
    {
        private readonly IContributorRepository _contributorRepository;
        private readonly IContributionRepository _contributionRepository;
        private readonly ILogger<AnonymiseContributorHandler> _logger;

        public AnonymiseContributorHandler(
            IContributorRepository contributorRepository,
            IContributionRepository contributionRepository,
            ILogger<AnonymiseContributorHandler> logger)
        {
            _contributorRepository = contributorRepository;
            _contributionRepository = contributionRepository;
            _logger = logger;
        }

        public async Task<Result> Handle(AnonymiseContributorCommand request, CancellationToken cancellationToken)
        {
            var contributor = await _contributorRepository.GetByPlatformUserIdAsync(request.UserId);
            if (contributor == null)
            {
                _logger.LogInformation("No contributor to anonymise for the deleted account");
                return Result.Success();
            }

            // The contributions are anonymised first: the contributor is found by the platform user id, which the
            // contributor step replaces, so a failure in between must leave the contributor retryable.
            await _contributionRepository.AnonymiseByContributorIdAsync(contributor.Id, cancellationToken);
            await _contributorRepository.AnonymiseAsync(contributor.Id, cancellationToken);

            _logger.LogInformation("Contributor {ContributorId} anonymised", contributor.Id);

            return Result.Success();
        }
    }
}