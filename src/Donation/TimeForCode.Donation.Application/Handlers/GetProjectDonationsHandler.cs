using MediatR;
using Microsoft.Extensions.Logging;
using TimeForCode.Donation.Application.Interfaces;
using TimeForCode.Donation.Commands;
using TimeForCode.Donation.Values;

namespace TimeForCode.Donation.Application.Handlers
{
    internal class GetProjectDonationsHandler : IRequestHandler<GetProjectDonationsQuery, Result<GetDonationsResult>>
    {
        private readonly IProjectRepository _projectRepository;
        private readonly IDonationRepository _donationRepository;
        private readonly ILogger<GetProjectDonationsHandler> _logger;

        public GetProjectDonationsHandler(
            IProjectRepository projectRepository,
            IDonationRepository donationRepository,
            ILogger<GetProjectDonationsHandler> logger)
        {
            _projectRepository = projectRepository;
            _donationRepository = donationRepository;
            _logger = logger;
        }

        public async Task<Result<GetDonationsResult>> Handle(GetProjectDonationsQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Getting donations of project {ProjectId}", request.ProjectId);

            var project = await _projectRepository.GetByIdAsync(request.ProjectId);
            if (project == null || !project.IsPublished)
            {
                _logger.LogWarning("Project {ProjectId} not found or not published", request.ProjectId);
                return Result<GetDonationsResult>.NotFound("Project not found.");
            }

            var donations = await _donationRepository.GetByProjectIdAsync(request.ProjectId, cancellationToken);

            return Result<GetDonationsResult>.Success(new GetDonationsResult
            {
                Donations = donations.Select(d => d.ToDto()).ToList()
            });
        }
    }
}