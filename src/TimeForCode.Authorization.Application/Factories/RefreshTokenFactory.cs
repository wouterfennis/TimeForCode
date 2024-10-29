using Microsoft.Extensions.Options;
using MongoDB.Bson;
using TimeForCode.Authorization.Application.Interfaces;
using TimeForCode.Authorization.Application.Options;
using TimeForCode.Authorization.Domain.Entities;

namespace TimeForCode.Authorization.Domain.Factories
{
    public class RefreshTokenFactory
    {
        private readonly int _defaultExpirationAfterInDays;
        private readonly IRandomGenerator _randomGenerator;
        private readonly TimeProvider _timeProvider;

        public RefreshTokenFactory(IRandomGenerator randomGenerator, TimeProvider timeProvider, IOptions<RefreshTokenOptions> options)
        {
            _defaultExpirationAfterInDays = options.Value.DefaultExpirationAfterInDays;
            _randomGenerator = randomGenerator;
            _timeProvider = timeProvider;
        }

        public RefreshToken Create()
        {
            var token = _randomGenerator.GenerateRandomString();

            return new RefreshToken
            {
                Id = ObjectId.GenerateNewId(),
                ExpiresAfter = _timeProvider.GetUtcNow().AddDays(_defaultExpirationAfterInDays),
                Token = token
            };
        }
    }
}
