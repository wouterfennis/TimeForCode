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
            var organization = await _organizationRepository.GetByIdAsync(request.DonorOrganizationId);
            if (organization == null)
            {
                _logger.LogWarning("Donor organization {DonorOrganizationId} not found", request.DonorOrganizationId);
                return Result<GetContributionsResult>.NotFound("Donor organization not found.");
            }

            if (!request.IsAdministrator)
            {
                var caller = await _contributorRepository.GetByPlatformUserIdAsync(request.CallerUserId);
                if (caller?.DonorOrganizationId?.ToString() != request.DonorOrganizationId)
                {
                    _logger.LogWarning("User {UserId} is not a member of donor organization {DonorOrganizationId}", request.CallerUserId, request.DonorOrganizationId);
                    return Result<GetContributionsResult>.Forbidden("Only members of the donor organization or an administrator can access its contributions.");
                }
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