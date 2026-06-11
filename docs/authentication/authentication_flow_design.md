
# Authentication Flow

---

## In short

1. User visits website.
2. User initiates login.
3. Website calls the internal authentication service and specifies the identity provider.
4. Internal authentication service creates and stores a `state` value.
5. User is redirected to the external identity provider for authentication.
6. The external identity provider redirects back with an authorization code and the original `state`.
7. Internal service exchanges the code for an external access token and retrieves the user profile.
8. Internal service links the external account to an internal user account.
9. Internal service issues a new internal access token and refresh token.
10. The internal token is used for subsequent API calls and validated on each request.

---

### 1. A person visits the website

- The user accesses the website, which is in an unauthenticated state at this point.
- They may be presented with public content or a login button if authentication is required for more features.

### 2. The person presses the login button

- The user initiates the authentication process by clicking the login button.
- The website redirects the user to the internal authentication service.

### 3. The website calls the internal authentication service

- The website calls the **internal authentication service** and specifies which external identity provider should be used.
- In the current implementation, the website explicitly starts login with GitHub.
- In a more dynamic setup, the website or authentication service could choose between providers such as GitHub or Google.

### 4. The internal authentication service creates a state value

- Before redirecting the user, the authentication service creates a random `state` value.
- The service stores the `state` together with the selected identity provider and the application's return URL.
- This `state` is sent to the external identity provider and later checked on callback.
- The purpose of `state` is to correlate the callback to the original login request and protect the flow against tampering or CSRF-style attacks.

### 5. The internal authentication service forwards to the identity provider

- The authentication service redirects the user to the **identity provider** OAuth 2.0 authorization endpoint.
- The redirect URL includes a `client_id`, `redirect_uri`, `scope`, and `state`.
- Example GitHub OAuth 2.0 URL:

```text
https://github.com/login/oauth/authorize?client_id=the_client_id
&redirect_uri=https://the-service.com/callback
&scope=user
&state=random_state_value
```

### 6. The identity provider forwards back to the internal authentication service

- After the user logs in with the **identity provider** and grants permission, the **identity provider** redirects the user back to the **internal authentication service**.
- The callback contains an authorization code and the original `state`.
- The internal authentication service checks that the `state` is known and matches a previously initiated login flow.
- The service then exchanges the authorization code for an access token by making a server-side request to the identity provider token endpoint.

### 7. The authentication service retrieves the external account details

- After receiving the external access token, the internal authentication service uses it to call the identity provider API and retrieve the user's profile details.
- In this kind of OAuth flow, the external access token is often treated as an opaque token, so the practical validation step is usually a successful call to the provider API or another provider-specific validation mechanism.
- The service uses the returned account information, such as provider account ID or email address, to identify the user.

### 8. The authentication service links the external account to an internal account

- The internal authentication service looks up the internal account associated with the external provider account.
- If an account already exists, it is reused.
- If no account exists, the service can create one or reject the login, depending on business rules.

### 9. A new internal token is created with application-specific claims

- Once the user is validated and linked to an internal account, the internal authentication service generates a new internal token for the application.
- In this solution, the internal token is a JWT signed by the service.
- The token contains internal claims such as the user identifier and application scope.
- A refresh token is also created so the user can obtain a new internal access token without logging in again.

### 10. The internal token is used on subsequent requests

- The internal access token is stored in a secure HttpOnly cookie.
- The refresh token is also stored in a secure HttpOnly cookie.
- When the website calls backend APIs, it reads the internal access token from the cookie and forwards it as a bearer token to downstream services.
- Backend services validate the internal JWT by checking:
  1. the token signature
  2. the token expiration
  3. the issuer and audience
  4. the required claims or scopes
- If the token is valid, the backend service processes the request.

---

### Points to consider when implementing

1. **State Handling**

   - Always create and validate a `state` value for the authorization flow.
   - Store enough information with the `state` to restore the intended
     post-login redirect and identity provider context.

1. **Token Expiration and Refresh**

   - Internal access tokens should be short-lived.
   - Use refresh tokens to keep the session alive without forcing the user
     to log in repeatedly.
   - Rotate refresh tokens when issuing new ones.

1. **Security**

   - Prefer secure HttpOnly cookies over local storage for browser-based
     authentication flows.
   - Use `Secure` cookies and enforce HTTPS.
   - Consider appropriate `SameSite` settings for the application flow.
   - Validate redirect URIs against an allow-list.

1. **Scopes and Claims**

   - Internal scopes and claims should reflect application permissions,
     not just external provider permissions.
   - Keep the internal authorization model separate from the external
     provider model.

1. **Single Sign-On (SSO)**

   - If multiple identity providers are supported, the authentication
     service should be able to map multiple external identities to a
     single internal account where required.

1. **Error Handling**

   - Handle cases such as invalid or expired authorization codes, unknown
     `state`, denied consent, invalid redirect URIs, expired refresh
     tokens, or missing internal account mappings.
