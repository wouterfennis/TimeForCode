# Personal data

This document lists the personal data the platform stores, why, and for how long, and how a user has it removed. It describes the implemented behaviour. The wording of purposes and the lawful basis is a technical description and **has not had a legal check**; it must be reviewed before the first production release (see [risk RISK-07](../architecture/arc42/11-risks-and-technical-debt.md)).

## Where personal data comes from

A user logs in with GitHub. The platform requests no OAuth scope (public profile only) and discards the GitHub access token after reading the profile (see [ADR-013](adr/0013-no-stored-github-tokens.md)). Everything below is data GitHub already publishes on the public profile, plus data the user creates on the platform.

## Authorization module

### Account (`AccountInformation`)

| Field | Purpose | Retention |
| --- | --- | --- |
| `IdentityProviderId` | Recognise the returning user at GitHub | Until the user deletes the account |
| `Login` | Display the user and put the GitHub login in the access token claim `login` | Until the user deletes the account |
| `NodeId` | GitHub's global identifier of the profile | Until the user deletes the account |
| `AvatarUrl` | Display the user and put the avatar in the access token claim `avatar_url` | Until the user deletes the account |
| `Name` | Display name on the profile | Until the user deletes the account |
| `Email` | Contact address of the profile; only filled when it is public on GitHub | Until the user deletes the account |
| `Company`, `Bio`, `Location` | Show the profile as on GitHub | Until the user deletes the account |

The profile is refreshed from GitHub at every login.

**There is no automatic deletion of idle accounts yet**: the retention of every field above is "until the user deletes the account", however long the user stays away. Only refresh tokens and login state expire on their own. Tracked in [#123](https://github.com/wouterfennis/TimeForCode/issues/123) and [risk RISK-10](../architecture/arc42/11-risks-and-technical-debt.md).

### Refresh token (`RefreshToken`)

| Field | Purpose | Retention |
| --- | --- | --- |
| `TokenHash` (SHA-256, never the token) | Keep the session alive without a new GitHub login | Until its expiry (7 days after issue or last rotation), removed by a TTL index; deleted at logout, replay detection and account deletion |
| `UserId` | Link the token to the account | Same as the token |
| `ExpiresAfter`, `RotatedAt` | Expiry and rotation bookkeeping | Same as the token |

### Login state (`StateEntry`)

A random state key, the redirect URI and the PKCE code verifier of a login in progress. Holds no personal data about the user. Removed by a TTL index after 10 minutes or consumed by the callback.

### Cookies

The browser holds one opaque cookie, `__Host-session` (`HttpOnly`, `Secure`, `SameSite=Strict`), that identifies the session at the BFF and contains no personal data. The tokens live in the session record of the BFF database (collection `BffSessions`, with the hash of the cookie value), which holds the access and refresh token and timestamps. The record is removed at sign-out, after 30 idle minutes or 8 hours, or at the latest by its TTL index. A short-lived `__Host-login` cookie and its record (`BffLoginAttempts`, the PKCE verifier and a local return path) exist while a sign-in is in progress. The admin passkey login uses the same session; its ceremony state is a short-lived server-side record (`CeremonyState`, hash of an opaque id, removed when used or after five minutes) and holds no personal data.

## Donation module

### Contributor (`Contributor`)

| Field | Purpose | Retention |
| --- | --- | --- |
| `PlatformUserId` | Link logged hours to one user | Replaced by `deleted-<contributor id>` at account deletion |
| `GithubLogin`, `AvatarUrl` | Show who contributed the hours | Replaced by the placeholder `deleted-user` and `https://github.com/ghost.png` at account deletion |

### Contribution (`Contribution`)

| Field | Purpose | Retention |
| --- | --- | --- |
| `ContributorGitHubLogin`, `ContributorAvatarUrl` (snapshot) | Show the contributor without a lookup; **publicly visible** on the project | Replaced like the contributor at account deletion |
| `Hours`, `GithubReference`, `LoggedAt`, `ProjectId` | Record the time spent and its evidence | Kept: the hours stay on the project after the account is deleted. The reference is a link to a public GitHub pull request or commit |

### Project (`Project`)

`PublishedByUserId` identifies the maintainer who registered a project and gates the maintainer operations. It is **not** removed at account deletion: the project and its public GitHub snapshot stay, and the identifier no longer resolves to any profile (see [risk RISK-08](../architecture/arc42/11-risks-and-technical-debt.md)).

## Account deletion

A signed-in user deletes the account with `DELETE /api/v1/user`:

1. The Authorization API asks the Donation API to anonymise the contributor of the user (`POST /api/v1/contributor/{userId}/anonymise`, with a service token, scope `service`; see [ADR-001](adr/0001-use-internal-jwt.md)). When this fails, the deletion fails with `502` and nothing is removed, so the user can retry.
2. All refresh tokens of the user are deleted.
3. The account record is deleted.
4. The BFF session of the browser is not removed by the deletion; its refresh token no longer exists, so the session ends at the next token refresh (at most 15 minutes later).

The profile is no longer found afterwards (`GET /api/v1/user` returns `404`) and a refresh with an old refresh token fails. Anonymising twice is accepted, so a retry after a partial failure completes the work.

There is no data export yet, and the platform keeps no backups of its own beyond those of the database service.
