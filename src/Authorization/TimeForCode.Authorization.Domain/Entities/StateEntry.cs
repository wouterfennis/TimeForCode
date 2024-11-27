using TimeForCode.Authorization.Values;

namespace TimeForCode.Authorization.Domain.Entities
{
    public class StateEntry
    {
        public string Key { get; private init; }
        public IdentityProvider IdentityProvider { get; private init; }
        public Uri RedirectUri { get; private init; }

        public StateEntry(string key, IdentityProvider identityProvider, Uri redirectUri)
        {
            Key = key;
            IdentityProvider = identityProvider;
            RedirectUri = redirectUri;
        }
    }
}