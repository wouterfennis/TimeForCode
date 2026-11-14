using MediatR;
using Microsoft.Extensions.Logging;
using System.Net;
using TimeForCode.Reporting.Application.Interfaces;
using TimeForCode.Reporting.Commands;
using TimeForCode.Reporting.Domain;

namespace TimeForCode.Reporting.Application.Handlers
{
    internal class DefineBadgeTierHandler : IRequestHandler<DefineBadgeTierCommand, Result<BadgeTierDto>>
    {
        /// <summary>Longest tier name accepted.</summary>
        internal const int MaxNameLength = 100;

        private readonly IBadgeTierRepository _tiers;
        private readonly ILogger<DefineBadgeTierHandler> _logger;

        public DefineBadgeTierHandler(IBadgeTierRepository tiers, ILogger<DefineBadgeTierHandler> logger)
        {
            _tiers = tiers;
            _logger = logger;
        }

        public async Task<Result<BadgeTierDto>> Handle(DefineBadgeTierCommand request, CancellationToken cancellationToken)
        {
            string name = request.Name.Trim();
            if (name.Length == 0 || name.Length > MaxNameLength)
            {
                return Result<BadgeTierDto>.Failure($"The tier name must be between 1 and {MaxNameLength} characters.", HttpStatusCode.BadRequest);
            }

            if (request.ThresholdHours <= 0)
            {
                return Result<BadgeTierDto>.Failure("The threshold must be a positive number of hours.", HttpStatusCode.BadRequest);
            }

            var tier = new BadgeTier(name, request.RecipientType, request.ThresholdHours);
            if (!await _tiers.SaveAsync(tier, cancellationToken))
            {
                _logger.LogWarning("Badge tier {TierName} was refused: another {RecipientType} tier already has {ThresholdHours} hours", name, request.RecipientType, request.ThresholdHours);
                return Result<BadgeTierDto>.Failure($"Another {request.RecipientType} tier already has a threshold of {request.ThresholdHours} hours.", HttpStatusCode.Conflict);
            }

            return Result<BadgeTierDto>.Success(new BadgeTierDto { Name = name, RecipientType = request.RecipientType, ThresholdHours = request.ThresholdHours });
        }
    }
}