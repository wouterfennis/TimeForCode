using MediatR;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using TimeForCode.Donation.Application.Interfaces;
using TimeForCode.Donation.Commands;

namespace TimeForCode.Donation.Application.Handlers
{
    internal class PledgeDonationHandler : IRequestHandler<PledgeDonationCommand, Result<DonationResult>>
    {
        private readonly IProjectRepository _projectRepository;
        private readonly IDonorOrganizationRepository _organizationRepository;
        private readonly IDonationRepository _donationRepository;
        private readonly ILogger<PledgeDonationHandler> _logger;

        public PledgeDonationHandler(
            IProjectRepository projectRepository,
            IDonorOrganizationRepository organizationRepository,
            IDonationRepository donationRepository,
            ILogger<PledgeDonationHandler> logger)
        {
            _projectRepository = projectRepository;
            _organizationRepository = organizationRepository;
            _donationRepository = donationRepository;
            _logger = logger;
        }

        public async Task<Result<DonationResult>> Handle(PledgeDonationCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Pledging {Hours} hours to project {ProjectId} for user {UserId}", request.Hours, request.ProjectId, request.UserId);

            var project = await _projectRepository.GetByIdAsync(request.ProjectId);
            if (project == null)
            {
                _logger.LogWarning("Project {ProjectId} not found", request.ProjectId);
                return Result<DonationResult>.NotFound("Project not found.");
            }

            var projectCheck = project.EnsureCanReceiveDonations();
            if (!projectCheck.IsSuccess)
            {
                _logger.LogWarning("Project {ProjectId} cannot receive donations", request.ProjectId);
                return Result<DonationResult>.Conflict(projectCheck.ErrorMessage);
            }

            ObjectId? organizationId = null;
            if (!string.IsNullOrWhiteSpace(request.DonorOrganizationId))
            {
                var organization = await _organizationRepository.GetByIdAsync(request.DonorOrganizationId);
                if (organization == null)
                {
                    _logger.LogWarning("Donor organization {OrganizationId} not found", request.DonorOrganizationId);
                    return Result<DonationResult>.NotFound("Donor organization not found.");
                }

                organizationId = organization.Id;
            }

            var donation = Domain.Donation.Create(project.Id, organizationId, request.UserId, request.Hours);
            await _donationRepository.CreateAsync(donation);

            _logger.LogInformation("Donation {DonationId} pledged to project {ProjectId}", donation.Id, project.Id);

            return Result<DonationResult>.Success(new DonationResult { Donation = donation.ToDto() });
        }
    }
}