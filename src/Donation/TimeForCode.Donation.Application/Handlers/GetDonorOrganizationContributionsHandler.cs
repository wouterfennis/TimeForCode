using MediatR;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using TimeForCode.Donation.Application.Interfaces;
using TimeForCode.Donation.Commands;

namespace TimeForCode.Donation.Application.Handlers
{
    internal class GetDonorOrganizationContributionsHandler : IRequestHandler<GetDonorOrganizationContributionsQuery, Result<GetDonorOrganizationContributionsResult>>
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

        public async Task<Result<GetDonorOrganizationContributionsResult>> Handle(GetDonorOrganizationContributionsQuery request, CancellationToken cancellationToken)
        {
            var denied = await DonorOrganizationAccess.CheckAsync<GetDonorOrganizationContributionsResult>(
                _organizationRepository, _contributorRepository, request.DonorOrganizationId, request.CallerUserId, request.IsAdministrator, _logger);
            if (denied != null)
            {
                return denied;
            }

            var contributors = await _contributorRepository.GetByDonorOrganizationIdAsync(ObjectId.Parse(request.DonorOrganizationId), cancellationToken);
            if (contributors.Count == 0)
            {
                return Result<GetDonorOrganizationContributionsResult>.Success(Page([], 0, request));
            }

            var (contributions, totalCount) = await _contributionRepository.GetPageByContributorIdsAsync(
                contributors.Select(c => c.Id).ToList(), request.PageNumber, request.PageSize, cancellationToken);

            return Result<GetDonorOrganizationContributionsResult>.Success(Page(contributions.Select(c => c.ToDto()).ToList(), totalCount, request));
        }

        private static GetDonorOrganizationContributionsResult Page(IReadOnlyList<ContributionDto> contributions, int totalCount, GetDonorOrganizationContributionsQuery request)
        {
            return new GetDonorOrganizationContributionsResult
            {
                Contributions = contributions,
                TotalCount = totalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            };
        }
    }
}