# Arc42 Section 6 — Runtime View

Status: Mixed

This section describes the most important runtime scenarios — how the building blocks interact at execution time.

---

## Scenario 1 — User Login via GitHub OAuth 2.0

This is the current, fully implemented authentication flow. See [authentication flow design](../../authentication/authentication_flow_design.md) for the complete step-by-step description.

```mermaid
sequenceDiagram
    participant Browser
    participant Website
    participant AuthAPI as Authorization API
    participant GitHub

    Browser->>Website: GET / (unauthenticated)
    Website-->>Browser: Login page
    Browser->>Website: Click "Sign in with GitHub"
    Website->>AuthAPI: GET /api/authentication/login
    AuthAPI-->>Browser: 302 Redirect to GitHub (with state, client_id, scope)
    Browser->>GitHub: GET /login/oauth/authorize
    GitHub-->>Browser: GitHub login & consent page
    Browser->>GitHub: User approves
    GitHub-->>AuthAPI: GET /api/authentication/callback?code=...&state=...
    AuthAPI->>GitHub: POST /login/oauth/access_token (exchange code)
    GitHub-->>AuthAPI: External access token
    AuthAPI->>GitHub: GET /user (fetch profile)
    GitHub-->>AuthAPI: User profile (id, login, email)
    AuthAPI->>AuthAPI: Link GitHub account to internal user
    AuthAPI->>AuthAPI: Issue internal JWT + refresh token
    AuthAPI-->>Browser: 302 Redirect to Website (set HttpOnly cookies)
    Browser->>Website: GET / (authenticated, JWT cookie present)
```

---

## Scenario 2 — Token Refresh

When the access token expires, the Website obtains a new one transparently.

```mermaid
sequenceDiagram
    participant Website
    participant AuthAPI as Authorization API

    Website->>AuthAPI: POST /api/authentication/refresh (refresh token cookie)
    AuthAPI->>AuthAPI: Validate refresh token (expiry, revocation)
    AuthAPI->>AuthAPI: Issue new access token + rotate refresh token
    AuthAPI-->>Website: 200 OK (new HttpOnly cookies set)
```

The Website does not need to re-direct the user; the `CookieAuthorizationHandler` in the Shared layer intercepts 401 responses and triggers the refresh automatically.

---

## Scenario 3 — Register a Project

```mermaid
sequenceDiagram
    participant Website
    participant DonationAPI as Donation API
    participant GitHub
    participant MongoDB

    Website->>DonationAPI: POST /api/v1/project { githubUrl } (JWT bearer)
    DonationAPI->>DonationAPI: Validate JWT (policy: ApiUser)
    DonationAPI->>GitHub: GET /repos/:owner/:repo
    GitHub-->>DonationAPI: Repository metadata (name, description, language, topics, ...)
    DonationAPI->>DonationAPI: Validate repository is public and not archived
    DonationAPI->>DonationAPI: Construct Project domain entity
    DonationAPI->>MongoDB: Insert Project { status: Published }
    DonationAPI-->>Website: 201 Created { projectId }
```

---

## Scenario 4 — Log Hours Against a Donation (Target)

```mermaid
sequenceDiagram
    participant Website
    participant DonationAPI as Donation API
    participant MongoDB

    Website->>DonationAPI: POST /api/v1/donation/{id}/transaction { hours, description } (JWT bearer)
    DonationAPI->>DonationAPI: Validate JWT (role: Contributor)
    DonationAPI->>MongoDB: Fetch Donation by id
    DonationAPI->>DonationAPI: Validate donation is Active and contributor is assigned
    DonationAPI->>DonationAPI: Create DonationTransaction
    DonationAPI->>MongoDB: Insert DonationTransaction
    DonationAPI->>DonationAPI: Update donation.hoursLogged
    alt hoursLogged >= hoursCommitted
        DonationAPI->>MongoDB: Update Donation { status: Completed }
    end
    DonationAPI-->>Website: 201 Created { transactionId, hoursLogged, status }
```
