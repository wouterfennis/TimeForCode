using MediatR;
using Microsoft.Extensions.Logging;
using System.Net;
using TimeForCode.Reporting.Application.Interfaces;
using TimeForCode.Reporting.Commands;

namespace TimeForCode.Reporting.Application.Handlers
{
    internal class RemoveBadgeTierHandler : IRequestHandler<RemoveBadgeTierCommand, Result<bool>>
    {
        private readonly IBadgeTierRepository _tiers;
        private readonly ILogger<RemoveBadgeTierHandler> _logger;

        public RemoveBadgeTierHandler(IBadgeTierRepository tiers, ILogger<RemoveBadgeTierHandler> logger)
        {
            _tiers = tiers;
            _logger = logger;
        }

        public async Task<Result<bool>> Handle(RemoveBadgeTierCommand request, CancellationToken cancellationToken)
        {
            if (!await _tiers.RemoveAsync(request.RecipientType, request.Name.Trim(), cancellationToken))
            {
                _logger.LogWarning("Badge tier {TierName} of {RecipientType} not found", request.Name, request.RecipientType);
                return Result<bool>.Failure("Badge tier not found.", HttpStatusCode.NotFound);
            }

            return Result<bool>.Success(true);
        }
    }
}