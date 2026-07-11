using MediatR;
using Microsoft.Extensions.Logging;
using TimeForCode.Donation.Application.Interfaces;
using TimeForCode.Donation.Commands;

namespace TimeForCode.Donation.Application.Handlers
{
    internal class DeleteDonorOrganizationHandler : IRequestHandler<DeleteDonorOrganizationCommand, Result<IDeleteDonorOrganizationResult>>
    {
        private readonly IDonorOrganizationRepository _repository;
        private readonly ILogger<DeleteDonorOrganizationHandler> _logger;

        public DeleteDonorOrganizationHandler(IDonorOrganizationRepository repository, ILogger<DeleteDonorOrganizationHandler> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<Result<IDeleteDonorOrganizationResult>> Handle(DeleteDonorOrganizationCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Deleting donor organization {Id}", request.Id);

            var organization = await _repository.GetByIdAsync(request.Id);
            if (organization == null)
            {
                _logger.LogWarning("Donor organization with id {Id} not found", request.Id);
                return Result<IDeleteDonorOrganizationResult>.Failure($"Donor organization with id '{request.Id}' was not found.");
            }

            await _repository.DeleteAsync(request.Id);

            _logger.LogInformation("Donor organization {Id} deleted successfully", request.Id);

            return Result<IDeleteDonorOrganizationResult>.Success(new IDeleteDonorOrganizationResult());
        }
    }
}