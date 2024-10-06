using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.Web;
using TimeForCode.Authorization.Application.Options;
using TimeForCode.Authorization.Commands;

namespace TimeForCode.Authorization.Application.Handlers
{
    public class LoginHandler : IRequestHandler<LoginCommand, Uri>
    {
        private readonly ExternalIdentityProviderOptions _options;
        private readonly IMemoryCache _memoryCache;

        public LoginHandler(IOptions<ExternalIdentityProviderOptions> options, IMemoryCache memoryCache)
        {
            _options = options.Value;
            _memoryCache = memoryCache;
        }

        public Task<Uri> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            var uriBuilder = new UriBuilder
            {
                Scheme = "https",
                Path = "/login/oauth/authorize"
            };

            string state = CreateState();

            var query = HttpUtility.ParseQueryString(string.Empty);
            query["state"] = state;
            query["redirect_uri"] = _options.CallbackUri;
            query["scope"] = "user";

            var identityProvider = _options.GetExternalIdentityProvider(request.IdentityProvider);

            uriBuilder.Host = identityProvider.Host;
            query["client_id"] = identityProvider.ClientId;
            uriBuilder.Query = query.ToString();
        }

        private string CreateState()
        {
            string state = Guid.NewGuid().ToString("N");
            _memoryCache.Set(state, state, TimeSpan.FromMinutes(10));

            return state;
        }
    }
}
