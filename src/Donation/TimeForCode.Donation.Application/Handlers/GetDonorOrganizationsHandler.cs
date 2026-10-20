using MediatR;
using Microsoft.Extensions.Logging;
using TimeForCode.Donation.Application.Interfaces;
using TimeForCode.Donation.Commands;

namespace TimeForCode.Donation.Application.Handlers
{
    internal class GetDonorOrganizationsHandler : IRequestHandler<GetDonorOrganizationsQuery, Result<GetDonorOrganizationsResult>>
    {
        private readonly IDonorOrganizationRepository _repository;
        private readonly IDonationRepository _donationRepository;
        private readonly ILogger<GetDonorOrganizationsHandler> _logger;

        public GetDonorOrganizationsHandler(IDonorOrganizationRepository repository, IDonationRepository donationRepository, ILogger<GetDonorOrganizationsHandler> logger)
        {
            _repository = repository;
            _donationRepository = donationRepository;
            _logger = logger;
        }

        public async Task<Result<GetDonorOrganizationsResult>> Handle(GetDonorOrganizationsQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Getting donor organizations page {PageNumber} with page size {PageSize}", request.PageNumber, request.PageSize);

            var (organizations, totalCount) = await _repository.GetAllAsync(request.PageNumber, request.PageSize);

            _logger.LogDebug("Retrieved {Count} donor organizations out of {TotalCount} total", organizations.Count, totalCount);

            var donations = await _donationRepository.GetDonationsAsync(organizations, cancellationToken);
            var dtos = organizations.Select(o => o.ToDto(donations)).ToList();

            return Result<GetDonorOrganizationsResult>.Success(new GetDonorOrganizationsResult
            {
                Organizations = dtos,
                TotalCount = totalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            });
        }
    }
}