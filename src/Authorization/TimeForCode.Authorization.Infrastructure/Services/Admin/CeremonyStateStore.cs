using Microsoft.Extensions.Options;
using TimeForCode.Authorization.Application.Interfaces.Admin;
using TimeForCode.Authorization.Application.Options;
using TimeForCode.Authorization.Application.Services;
using TimeForCode.Authorization.Domain.Entities;

namespace TimeForCode.Authorization.Infrastructure.Services.Admin
{
    /// <summary>
    /// Keeps the state of passkey ceremonies on the server, single use and short lived. The browser only gets an opaque ceremony id,
    /// so no cookie is needed and a captured id and assertion cannot be replayed. Several instances share the state through the database.
    /// </summary>
    public class CeremonyStateStore(ICeremonyStateRepository repository, TimeProvider timeProvider, IOptions<AdminPasskeyOptions> options)
    {
        private readonly AdminPasskeyOptions _options = options.Value;

        /// <summary>
        /// Stores the state and returns the ceremony id the client sends back when it completes the ceremony.
        /// </summary>
        public async Task<string> StartAsync(CeremonyKind kind, string state)
        {
            var id = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32))
                .TrimEnd('=').Replace('+', '-').Replace('/', '_');

            await repository.AddAsync(new CeremonyState
            {
                Id = MongoDB.Bson.ObjectId.GenerateNewId(),
                IdHash = RefreshTokenHasher.Hash(id),
                Kind = kind,
                State = state,
                ExpiresAfter = timeProvider.GetUtcNow().AddSeconds(_options.CeremonyLifetimeInSeconds)
            });

            return id;
        }

        /// <summary>
        /// Takes the state of the ceremony: once only, of the right kind and before it expires; otherwise null. The state is consumed even then.
        /// </summary>
        public async Task<string?> TakeAsync(CeremonyKind kind, string? ceremonyId)
        {
            if (string.IsNullOrEmpty(ceremonyId))
            {
                return null;
            }

            var stored = await repository.TakeAsync(RefreshTokenHasher.Hash(ceremonyId));
            if (stored is null || stored.Kind != kind || stored.IsExpired(timeProvider.GetUtcNow()))
            {
                return null;
            }

            return stored.State;
        }
    }
}