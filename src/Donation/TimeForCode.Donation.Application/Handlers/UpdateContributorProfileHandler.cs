using MediatR;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using TimeForCode.Donation.Application.Interfaces;
using TimeForCode.Donation.Commands;
using TimeForCode.Donation.Domain;

namespace TimeForCode.Donation.Application.Handlers
{
    internal class UpdateContributorProfileHandler : IRequestHandler<UpdateContributorProfileCommand, Result<ContributorProfileResult>>
    {
        private readonly IContributorRepository _contributorRepository;
        private readonly IDonorOrganizationRepository _organizationRepository;
        private readonly ILogger<UpdateContributorProfileHandler> _logger;

        public UpdateContributorProfileHandler(
            IContributorRepository contributorRepository,
            IDonorOrganizationRepository organizationRepository,
            ILogger<UpdateContributorProfileHandler> logger)
        {
            _contributorRepository = contributorRepository;
            _organizationRepository = organizationRepository;
            _logger = logger;
        }

        public async Task<Result<ContributorProfileResult>> Handle(UpdateContributorProfileCommand request, CancellationToken cancellationToken)
        {
            Contributor? contributor = request.ContributorId == null
                ? await GetOrRegisterOwnContributorAsync(request)
                : await _contributorRepository.GetByIdAsync(ObjectId.Parse(request.ContributorId));

            if (contributor == null)
            {
                _logger.LogWarning("Contributor to update not found");
                return Result<ContributorProfileResult>.NotFound("Contributor not found.");
            }

            if (!contributor.MayAccess(request.CallerUserId, request.IsAdministrator))
            {
                _logger.LogWarning("Contributor {ContributorId} update refused: caller is neither the owner nor an administrator", contributor.Id);
                return Result<ContributorProfileResult>.Forbidden("Only the contributor or an administrator can change this profile.");
            }

            ObjectId? organizationId = null;
            if (request.DonorOrganizationId != null)
            {
                if (await _organizationRepository.GetByIdAsync(request.DonorOrganizationId) == null)
                {
                    _logger.LogWarning("Donor organization {DonorOrganizationId} not found", request.DonorOrganizationId);
                    return Result<ContributorProfileResult>.NotFound("Donor organization not found.");
                }

                organizationId = ObjectId.Parse(request.DonorOrganizationId);
            }

            contributor.UpdateProfile(request.Role, request.Skills, request.HoursAvailable, organizationId);
            await _contributorRepository.UpdateProfileAsync(contributor, cancellationToken);

            _logger.LogInformation("Profile of contributor {ContributorId} updated", contributor.Id);

            return Result<ContributorProfileResult>.Success(new ContributorProfileResult { Profile = contributor.ToProfileDto() });
        }

        private async Task<Contributor> GetOrRegisterOwnContributorAsync(UpdateContributorProfileCommand request)
        {
            var existing = await _contributorRepository.GetByPlatformUserIdAsync(request.CallerUserId);

            return existing ?? await _contributorRepository.UpsertAsync(
                Contributor.Create(request.CallerUserId, request.GithubLogin, request.AvatarUrl));
        }
    }
}