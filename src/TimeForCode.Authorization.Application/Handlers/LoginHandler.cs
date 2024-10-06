using MediatR;
using Microsoft.Extensions.Options;
using System.Web;
using TimeForCode.Authorization.Application.Options;
using TimeForCode.Authorization.Commands;

namespace TimeForCode.Authorization.Application.Handlers
{
    public class LoginHandler : IRequestHandler<LoginCommand, Uri>
    {
        private readonly ExternalIdentityProviderOptions _options;

        public LoginHandler(IOptions<ExternalIdentityProviderOptions> options)
        {
            _options = options.Value;
        }

        public Task<Uri> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            var uriBuilder = new UriBuilder
            {
                Scheme = "https",
                Path = "/login/oauth/authorize"
            };

            var query = HttpUtility.ParseQueryString(string.Empty);
            query["state"] = Guid.NewGuid().ToString();
            query["redirect_uri"] = _options.CallbackUri;
            query["scope"] = "user";

            switch (request.IdentityProvider)
            {
                case Values.IdentityProvider.Github:
                    uriBuilder.Host = "github.com";
                    query["client_id"] = _options.Github.ClientId;
                    uriBuilder.Query = query.ToString();

                    return Task.FromResult(uriBuilder.Uri);
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
