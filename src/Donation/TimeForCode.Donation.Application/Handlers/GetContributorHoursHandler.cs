using MediatR;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using TimeForCode.Donation.Application.Interfaces;
using TimeForCode.Donation.Commands;

namespace TimeForCode.Donation.Application.Handlers
{
    internal class GetContributorHoursHandler : IRequestHandler<GetContributorHoursQuery, Result<ContributorHoursResult>>
    {
        private readonly IContributorRepository _contributorRepository;
        private readonly IContributionRepository _contributionRepository;
        private readonly ILogger<GetContributorHoursHandler> _logger;

        public GetContributorHoursHandler(
            IContributorRepository contributorRepository,
            IContributionRepository contributionRepository,
            ILogger<GetContributorHoursHandler> logger)
        {
            _contributorRepository = contributorRepository;
            _contributionRepository = contributionRepository;
            _logger = logger;
        }

        public async Task<Result<ContributorHoursResult>> Handle(GetContributorHoursQuery request, CancellationToken cancellationToken)
        {
            var contributor = ObjectId.TryParse(request.ContributorId, out var contributorId)
                ? await _contributorRepository.GetByIdAsync(contributorId)
                : null;
            if (contributor == null)
            {
                _logger.LogWarning("Contributor {ContributorId} not found", request.ContributorId);
                return Result<ContributorHoursResult>.NotFound("Contributor not found.");
            }

            var totalHours = await _contributionRepository.GetTotalHoursByContributorIdAsync(contributorId, cancellationToken);
            return Result<ContributorHoursResult>.Success(new ContributorHoursResult { TotalHours = totalHours });
        }
    }
}