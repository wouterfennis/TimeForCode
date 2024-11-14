namespace TimeForCode.Authorization.Domain
{
    public static class OAuthConstants
    {
        public const string AuthorizationEndpoint = "/login/oauth/authorize";
        public const string TokenEndpoint = "/login/oauth/token";
        public const string CallbackEndpoint = "/login/oauth/callback";
        public const string AccessTokenEndpoint = "/login/oauth/access_token";
        public const string Scope = "scope";
        public const string State = "state";
        public const string RedirectUri = "redirect_uri";
        public const string ClientId = "client_id";
        public const string UserScope = "user";
        public const string AuthorizationHeader = "Authorization";
        public const string BearerPrefix = "Bearer ";
    }
}