using MediatR;
using TimeForCode.Reporting.Application.Interfaces;
using TimeForCode.Reporting.Commands;

namespace TimeForCode.Reporting.Application.Handlers
{
    internal class GetBadgeTiersHandler : IRequestHandler<GetBadgeTiersQuery, Result<IReadOnlyList<BadgeTierDto>>>
    {
        private readonly IBadgeTierRepository _tiers;

        public GetBadgeTiersHandler(IBadgeTierRepository tiers)
        {
            _tiers = tiers;
        }

        public async Task<Result<IReadOnlyList<BadgeTierDto>>> Handle(GetBadgeTiersQuery request, CancellationToken cancellationToken)
        {
            var tiers = await _tiers.ListAsync(request.RecipientType, cancellationToken);
            return Result<IReadOnlyList<BadgeTierDto>>.Success(
                tiers.Select(t => new BadgeTierDto { Name = t.Name, RecipientType = t.RecipientType, ThresholdHours = t.ThresholdHours }).ToList());
        }
    }
}