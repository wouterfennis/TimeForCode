using MediatR;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using TimeForCode.Donation.Application.Interfaces;
using TimeForCode.Donation.Commands;

namespace TimeForCode.Donation.Application.Handlers
{
    internal class GetDonorOrganizationContributionsHandler : IRequestHandler<GetDonorOrganizationContributionsQuery, Result<GetContributionsResult>>
    {
        private readonly IDonorOrganizationRepository _organizationRepository;
        private readonly IContributorRepository _contributorRepository;
        private readonly IContributionRepository _contributionRepository;
        private readonly ILogger<GetDonorOrganizationContributionsHandler> _logger;

        public GetDonorOrganizationContributionsHandler(
            IDonorOrganizationRepository organizationRepository,
            IContributorRepository contributorRepository,
            IContributionRepository contributionRepository,
            ILogger<GetDonorOrganizationContributionsHandler> logger)
        {
            _organizationRepository = organizationRepository;
            _contributorRepository = contributorRepository;
            _contributionRepository = contributionRepository;
            _logger = logger;
        }

        public async Task<Result<GetContributionsResult>> Handle(GetDonorOrganizationContributionsQuery request, CancellationToken cancellationToken)
        {
            var denied = await DonorOrganizationAccess.CheckAsync<GetContributionsResult>(
                _organizationRepository, _contributorRepository, request.DonorOrganizationId, request.CallerUserId, request.IsAdministrator, _logger);
            if (denied != null)
            {
                return denied;
            }

            var contributors = await _contributorRepository.GetByDonorOrganizationIdAsync(ObjectId.Parse(request.DonorOrganizationId), cancellationToken);
            if (contributors.Count == 0)
            {
                return Result<GetContributionsResult>.Success(new GetContributionsResult { Contributions = [] });
            }

            var contributions = await _contributionRepository.GetByContributorIdsAsync(contributors.Select(c => c.Id).ToList(), cancellationToken);

            return Result<GetContributionsResult>.Success(new GetContributionsResult
            {
                Contributions = contributions.Select(c => c.ToDto()).ToList()
            });
        }
    }
}