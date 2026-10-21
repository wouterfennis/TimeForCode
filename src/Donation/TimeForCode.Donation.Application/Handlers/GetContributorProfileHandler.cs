using MediatR;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using TimeForCode.Donation.Application.Interfaces;
using TimeForCode.Donation.Commands;
using TimeForCode.Donation.Domain;

namespace TimeForCode.Donation.Application.Handlers
{
    internal class GetContributorProfileHandler : IRequestHandler<GetContributorProfileQuery, Result<ContributorProfileResult>>
    {
        private readonly IContributorRepository _contributorRepository;
        private readonly ILogger<GetContributorProfileHandler> _logger;

        public GetContributorProfileHandler(IContributorRepository contributorRepository, ILogger<GetContributorProfileHandler> logger)
        {
            _contributorRepository = contributorRepository;
            _logger = logger;
        }

        public async Task<Result<ContributorProfileResult>> Handle(GetContributorProfileQuery request, CancellationToken cancellationToken)
        {
            Contributor? contributor = request.ContributorId == null
                ? await _contributorRepository.GetByPlatformUserIdAsync(request.CallerUserId)
                : await _contributorRepository.GetByIdAsync(ObjectId.Parse(request.ContributorId));

            if (contributor == null)
            {
                _logger.LogWarning("Contributor profile not found");
                return Result<ContributorProfileResult>.NotFound("Contributor not found.");
            }

            if (!contributor.MayAccess(request.CallerUserId, request.IsAdministrator))
            {
                _logger.LogWarning("Contributor {ContributorId} requested by a user who may not see the profile", contributor.Id);
                return Result<ContributorProfileResult>.Forbidden("Only the contributor or an administrator can access this profile.");
            }

            return Result<ContributorProfileResult>.Success(new ContributorProfileResult { Profile = contributor.ToProfileDto() });
        }
    }
}