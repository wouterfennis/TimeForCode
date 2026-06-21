# ADR-001 — Use internal JWT instead of forwarding GitHub tokens

**Date**: 2025
**Status**: Accepted

### Context

TimeForCode uses GitHub OAuth 2.0 for authentication. After login, downstream services (e.g. the Donation API) need to authenticate requests from the Website. The question was whether to forward the GitHub access token to downstream services or issue an internal token.

### Decision

The Authorization API issues its own internal JWT (RS256-signed) after a successful GitHub login. Downstream services validate this internal token. The GitHub access token is never forwarded outside the Authorization API.

### Consequences

**Positive**: Downstream services are decoupled from GitHub. Adding a second identity provider (e.g. Google) requires no changes to downstream services. Internal claims (roles, user ID) are controlled by the platform, not GitHub.

**Negative**: An additional token issuance step adds complexity to the login flow. The Authorization API becomes a trust anchor that must be kept secure.
