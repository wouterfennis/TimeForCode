using MediatR;
using Microsoft.Extensions.Logging;
using TimeForCode.Donation.Application.Interfaces;
using TimeForCode.Donation.Commands;

namespace TimeForCode.Donation.Application.Handlers
{
    internal class GetMaintainerProfileHandler : IRequestHandler<GetMaintainerProfileQuery, Result<MaintainerProfileResult>>
    {
        private readonly IMaintainerRepository _maintainerRepository;
        private readonly ILogger<GetMaintainerProfileHandler> _logger;

        public GetMaintainerProfileHandler(IMaintainerRepository maintainerRepository, ILogger<GetMaintainerProfileHandler> logger)
        {
            _maintainerRepository = maintainerRepository;
            _logger = logger;
        }

        public async Task<Result<MaintainerProfileResult>> Handle(GetMaintainerProfileQuery request, CancellationToken cancellationToken)
        {
            var maintainer = await _maintainerRepository.GetByPlatformUserIdAsync(request.UserId);
            if (maintainer == null)
            {
                _logger.LogWarning("Maintainer of user {UserId} not found", request.UserId);
                return Result<MaintainerProfileResult>.NotFound("Maintainer not found.");
            }

            return Result<MaintainerProfileResult>.Success(new MaintainerProfileResult
            {
                Profile = new MaintainerProfileDto
                {
                    Id = maintainer.Id.ToString(),
                    GithubLogin = maintainer.GithubLogin,
                    AvatarUrl = maintainer.AvatarUrl
                }
            });
        }
    }
}