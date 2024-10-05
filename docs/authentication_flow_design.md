
# Authentication Flow

---

### In short:
1. User visits website.
2. User initiates login.
3. Website interacts with the internal authentication service.
4. User is redirected to the external identity provider for authentication.
5. The external identity provider redirects back with a token (or authorization code).
6. Internal service validates the token and links it to an internal user account.
7. Internal service issues a new internal token with appropriate scopes.
8. The internal token is used for subsequent API calls and validated on each request.

---

### 1. A person visits the website
- The user accesses the website, which is in an unauthenticated state at this point.
- They may be presented with public content or a "login" button if authentication is required for more features.

### 2. The person presses the login button
- The user initiates the authentication process by clicking the login button. This triggers the authentication flow.
- The user is redirected to the internal authentication service.

### 3. The website checks with the internal authentication service what needs to be done
- The website calls the **internal authentication service**, which determines the appropriate **identity provider** to use for authentication.
- This could be GitHub, Google, or any other provider based on the logic or configuration (e.g., GitHub for developers, Google for users).

### 4. The internal authentication service forwards the client to the external identity provider
- The authentication service **redirects the user** to the **identity provider** OAuth 2.0 authorization endpoint.
- The redirect URL includes a **client_id**, **redirect_uri** (which points back to the service), and any **scopes** needed for the **identity provider** (e.g., `user:email`).
- Example GitHub OAuth 2.0 URL:  
  `https://github.com/login/oauth/authorize?client_id=the_client_id&redirect_uri=https://the-service.com/callback&scope=user`

### 5. The identity provider forwards the client back to the internal authentication service with the external token
- After the user logs in with the **identity provider** and grants permission, The **identity provider** redirects the user back to the **internal authentication service** with an **authorization code**.
- The service exchanges the **authorization code** for an **access token** from the **identity provider** by making a server-side request to GitHub's token endpoint.
- The **identity provider** responds with an access token that the internal service can use to retrieve the user’s profile or email.

### 6. The authentication service will check the new token and look up an internal account that is stored for the person
- The **internal authentication service** now:
  1. **Validates the identity provider token** by verifying its signature and/or making a request to the **identity provider** API to retrieve the user's details (e.g., email, account ID).
  2. **Looks up the internal account** associated with the **identity provider** user by using the account ID or email to match against the internal database.
  3. If no account exists, it can create a new account or return an error.

### 7. A new internal token is created that contains the scopes the person needs to use the website
- Once the user is validated and an internal account is matched (or created), the **internal authentication service** generates a **new internal token** (JWT) for the user.
- This token:
  - Includes the necessary **claims** (e.g., user ID, roles, permissions).
  - Contains the appropriate **scopes** that define what the user can do within the application (e.g., `read`, `write`, `admin`).
  - Is signed with the service’s private key.
- This token is sent back to the client (browser or app), typically stored in a **cookie** or **local storage** (depending on the security policies).

### 8. When a new call is made, the internal token is validated
- On every subsequent request, the client sends the internal token (e.g., in an `Authorization: Bearer <token>` header).
- the backend services validate the token by:
  1. Checking the **signature** to ensure it’s legitimate.
  2. Verifying the token's **expiration** and **claims**.
  3. Ensuring that the **scopes** associated with the token allow the user to perform the requested operation.
- If the token is valid, the backend service processes the request.

---

### Points to consider when implementing:
1. **Token Expiration and Refresh:**  
   - **token expiration** must be handled. For longer-lived sessions, use **refresh tokens** so the user doesn’t have to log in frequently.
   - When the internal token expires, the client can request a new token using a refresh token.

2. **Security:**  
   - Ensure **secure storage** of the internal token on the client side (e.g., using **HttpOnly cookies** to prevent XSS attacks).
   - Use **HTTPS** to ensure tokens aren’t transmitted over insecure channels.

3. **Single Sign-On (SSO):**  
   - If multiple identity providers (e.g., GitHub, Google) are be allowed, the **internal authentication service** should be able to handle multiple external providers and map them to a single internal account.

4. **Scopes and Claims:**  
   - The internal token's **scopes** and **claims** should be designed to give users the right level of access based on their roles or permissions.

5. **Error Handling:**  
   - Ensure that the authentication service handles edge cases gracefully, such as when the GitHub token is invalid, the user denies access, or there’s no matching internal account.
