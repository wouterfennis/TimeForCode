# Page Specifications

Status: Mixed

Each entry states the purpose, data source, required states, and a low-fidelity wireframe. Wireframes show structure and priority, not final visuals. Field names come from the OpenAPI snapshots; check them before building.

Every page must implement these states: **loading** (skeleton, not a spinner alone), **empty**, **error with retry**, and where relevant **unauthorized**. See [information architecture](information-architecture.md#access-rules-in-the-ui).

Keep these `data-testid` values; the existing E2E specifications rely on them: `project-list`, `project-tile`, `project-link`, `user-name`, `login-handle`, `logout-link`, `new-user-welcome`, `not-logged-in-message`, `admin-nav-link`. Review the step definitions in `tst/Website/TimeForCode.Website.Specifications/Steps` for the complete set.

---

## Home — `/`

**Purpose**: explain the product in one screen and route each persona to their next step.
**Personas**: all (see [Personas and Journeys](../target/personas-and-journeys.md)).
**Data**: current user (`GET /api/v1/User`) to decide between signed-in and anonymous variants.

```text
+--------------------------------------------------------------+
| [Logo] TimeForCode        Projects      [Sign in with GitHub]|
+--------------------------------------------------------------+
|  Donate developer time to open source.                       |
|  [Browse projects]   [List your project]                     |
|                                                              |
|  How it works:  1 Find   2 Pledge   3 Track                  |
+--------------------------------------------------------------+
|  Featured projects (3 cards)                                 |
+--------------------------------------------------------------+
```

- Signed-in variant: greeting with the user name, and links to Profile and Sign out.
- A new user (first login) sees a one-time welcome message linking to the profile. Today the signal is an `IsNewUser` cookie set by the Authorization API, but it is HttpOnly, so the SPA cannot read it. **Open question**: the callback should return the flag in the redirect or a response field, or the BFF should expose it (see [architecture](architecture.md#authentication-and-the-bff)).
- The admin passkey sign-in is intentionally low-key (footer or a separate link), not a primary call to action.

## Project list — `/projects`

**Purpose**: let a visitor find a project worth supporting.
**Data**: `GET /api/v1/project` (paginated, anonymous).

```text
+--------------------------------------------------------------+
| Projects                              [Search] [Language v]  |
+--------------------------------------------------------------+
| +-------------------+ +-------------------+ +---------------+|
| | owner/repo        | | owner/repo        | | ...           ||
| | Description...    | | Description...    | |               ||
| | C#  *1.2k  GitHub | | Go   *300  GitHub | |               ||
| +-------------------+ +-------------------+ +---------------+|
| Showing 12 of 48              [< Prev] 1 2 3 [Next >]        |
+--------------------------------------------------------------+
```

- Card content today: full name, description, language, star count, link to GitHub.
- **Open question**: the API currently has paging only. Search and language filter require backend support; build the controls only when it exists, otherwise omit them.
- Real pagination or "load more"; reflect the page in the URL so results are shareable.

## Project detail — `/projects/:id`

**Purpose**: give enough information to decide to support a project.
**Data**: `GET /api/v1/project/{id}` (anonymous).

```text
+--------------------------------------------------------------+
| Projects > owner/repo                                        |
| owner/repo                         [View on GitHub]          |
| Description                                                  |
| C#   *1.2k stars   Open issues 34   License MIT              |
+--------------------------------------------------------------+
| Needs help with: (Planned)                                   |
| Pledge hours  [disabled until donation API exists]           |
+--------------------------------------------------------------+
```

- The owner sees an **Unpublish** action (`DELETE /api/v1/project/{id}`), with a confirmation dialog. A non-owner never sees it; a 403 from the API is still handled.
- Set the document title and a description meta tag per project, so shared links look good.

## Publish a project — `/projects/new`

**Purpose**: Journey 2 (maintainer registers a project).
**Data**: list the user's repositories with `GET /api/v1/User/repositories`; submit with `POST /api/v1/project`.

- Step 1: choose one of the user's public repositories, or paste a GitHub URL.
- Step 2: confirm the metadata fetched from GitHub, then publish.
- Handle: 400 (private or archived repository: explain why), 409 (already registered: link to the existing project).
- Check the OpenAPI snapshot for the exact request shape before building.

## Profile — `/profile`

**Purpose**: show who the user is and what they have on the platform.
**Data**: `GET /api/v1/User`.

```text
+--------------------------------------------------------------+
| (avatar)  Name                                               |
|           @login                                             |
| Bio / Location / ...                                         |
| My projects  (Planned: my donations)                         |
+--------------------------------------------------------------+
```

- Anonymous visitors see a sign-in prompt (`not-logged-in-message`), not an error.

## Admin home — `/admin`

**Purpose**: entry point for the platform operator (Robin).
**Access**: `admin` role.

- Current content: a landing page with links to the donor organization screens.
- Planned: a review queue for projects awaiting approval (Journey 4). Not buildable until the approval endpoints exist.

## Donor organizations — `/admin/donor-organizations`, `/new`, `/:id/edit`

**Purpose**: manage donor organizations.
**Data**: `GET/POST /api/v1/donororganization`, `GET/PUT/DELETE /api/v1/donororganization/{id}`.

```text
+--------------------------------------------------------------+
| Donor organizations                      [+ Add organization]|
+--------------------------------------------------------------+
| Name           | Industry | Hours | Actions                  |
| Acme BV        | Software |  120  | Edit   Delete            |
+--------------------------------------------------------------+
```

- Table with paging, an accessible row action menu, and a confirmation dialog before delete (204 on success).
- Form: inline field validation messages; map API 400 `ProblemDetails` to field errors, and 409 to a "name already exists" message on the name field.
- 404 on edit shows a not-found state.

## Admin sign-in (passkey)

**Purpose**: sign in as admin, or claim the admin role on first use.
**Data**: the four `admin-authentication` endpoints (registration/options, registration, authentication/options, authentication).

- Port the behaviour of `wwwroot/js/webauthn-admin-interop.js` into a typed module using the `navigator.credentials` API.
- Requests use `credentials: "include"` so the HttpOnly cookie round-trips.
- Registration needs the bootstrap secret; never store it, never log it.
- See [ADR-011](../reference/adr/0011-webauthn-admin-authentication.md) for the ceremony and its accepted risks. The Relying Party domain is configured as the Website origin, so the new front-end's production origin must be configured on the backend.

## Error and not-found pages

A friendly not-found page and a generic error page with a link home. Do not show stack traces or raw API messages.

---

## Planned pages (do not build yet)

| Page | Needs backend | Journey |
| --- | --- | --- |
| Pledge hours to a project | Donation endpoints | Journey 1 |
| Contributor dashboard and time logging | Contributor and transaction endpoints | Journey 3 |
| Admin project review queue | Project approval endpoints | Journey 4 |
| Organization impact report | Report endpoint | Journey 5 |

Specify each of these here, in the same format, when its API lands.
