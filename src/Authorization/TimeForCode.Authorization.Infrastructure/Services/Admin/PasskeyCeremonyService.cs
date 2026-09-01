using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using TimeForCode.Authorization.Application.Interfaces;
using TimeForCode.Authorization.Application.Interfaces.Admin;
using TimeForCode.Authorization.Application.Options;
using TimeForCode.Authorization.Commands.Admin;

namespace TimeForCode.Authorization.Infrastructure.Services.Admin
{
    /// <summary>
    /// The sole place <c>IPasskeyHandler&lt;TUser&gt;</c> is referenced. Wraps the ASP.NET Core Identity
    /// passkey ceremony behind <see cref="IPasskeyCeremonyService"/> so no Identity type leaks past this
    /// class. The ceremony state returned by the framework is encrypted with the existing
    /// <see cref="IEncryptionService"/> before being handed back, so it can be round-tripped safely via an
    /// HttpOnly cookie by the caller.
    /// </summary>
    public class PasskeyCeremonyService : IPasskeyCeremonyService
    {
        private readonly IPasskeyHandler<AdminPasskeyUser> _passkeyHandler;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IEncryptionService _encryptionService;
        private readonly AdminPasskeyOptions _options;

        public PasskeyCeremonyService(IPasskeyHandler<AdminPasskeyUser> passkeyHandler,
            IHttpContextAccessor httpContextAccessor,
            IEncryptionService encryptionService,
            IOptions<AdminPasskeyOptions> options)
        {
            _passkeyHandler = passkeyHandler;
            _httpContextAccessor = httpContextAccessor;
            _encryptionService = encryptionService;
            _options = options.Value;
        }

        public async Task<PasskeyCeremonyOptionsResult> CreateRegistrationOptionsAsync()
        {
            var userEntity = new PasskeyUserEntity
            {
                Id = AdminPasskeyUser.AdminUserId,
                Name = AdminPasskeyUser.AdminUserId,
                DisplayName = _options.RelyingPartyName
            };

            var creationOptions = await _passkeyHandler.MakeCreationOptionsAsync(userEntity, HttpContext);

            return new PasskeyCeremonyOptionsResult
            {
                OptionsJson = creationOptions.CreationOptionsJson,
                ProtectedCeremonyState = _encryptionService.Encrypt(creationOptions.AttestationState ?? string.Empty)
            };
        }

        public async Task<AdminAttestationOutcome> CompleteRegistrationAsync(string credentialJson, string ceremonyState)
        {
            var context = new PasskeyAttestationContext
            {
                HttpContext = HttpContext,
                CredentialJson = credentialJson,
                AttestationState = _encryptionService.Decrypt(ceremonyState)
            };

            var result = await _passkeyHandler.PerformAttestationAsync(context);

            if (!result.Succeeded || result.Passkey == null)
            {
                return new AdminAttestationOutcome { Succeeded = false, ErrorMessage = result.Failure?.Message ?? "Attestation failed." };
            }

            var passkey = result.Passkey;
            return new AdminAttestationOutcome
            {
                Succeeded = true,
                CredentialId = passkey.CredentialId,
                PublicKey = passkey.PublicKey,
                SignCount = passkey.SignCount,
                Transports = passkey.Transports,
                IsUserVerified = passkey.IsUserVerified,
                IsBackupEligible = passkey.IsBackupEligible,
                IsBackedUp = passkey.IsBackedUp
            };
        }

        public async Task<PasskeyCeremonyOptionsResult> CreateAuthenticationOptionsAsync()
        {
            var requestOptions = await _passkeyHandler.MakeRequestOptionsAsync(new AdminPasskeyUser(), HttpContext);

            return new PasskeyCeremonyOptionsResult
            {
                OptionsJson = requestOptions.RequestOptionsJson,
                ProtectedCeremonyState = _encryptionService.Encrypt(requestOptions.AssertionState ?? string.Empty)
            };
        }

        public async Task<AdminAssertionOutcome> CompleteAuthenticationAsync(string credentialJson, string ceremonyState)
        {
            var context = new PasskeyAssertionContext
            {
                HttpContext = HttpContext,
                CredentialJson = credentialJson,
                AssertionState = _encryptionService.Decrypt(ceremonyState)
            };

            var result = await _passkeyHandler.PerformAssertionAsync(context);

            if (!result.Succeeded || result.Passkey == null)
            {
                return new AdminAssertionOutcome { Succeeded = false, ErrorMessage = result.Failure?.Message ?? "Assertion failed." };
            }

            return new AdminAssertionOutcome { Succeeded = true, SignCount = result.Passkey.SignCount };
        }

        private HttpContext HttpContext => _httpContextAccessor.HttpContext!;
    }
}
