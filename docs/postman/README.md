# Postman: run the local stack

Collection for trying the Reporting API (impact report, badges, statistics, project reports) against the local stack.

## Start the stack

Start it with a known admin secret, from the repository root, in PowerShell 7:

```powershell
$env:TEST_ADMIN_ACCESS_SECRET = "choose-a-secret"
./scripts/start-local.ps1
```

Add `-ResetData` after a breaking data change (ADR-014). Ports: Authorization 8080, identity mock 8081, Donation 8082, Reporting 8083, frontend 8084.

## Use the collection

1. Import `TimeForCode-local.postman_collection.json`.
2. Set the collection variable `adminSecret` to your `TEST_ADMIN_ACCESS_SECRET`.
3. Run request 1. It stores the admin token in `token`.
4. Fill `orgId` and `projectId` with ids from the Donation API, or fill the stack first with `dotnet run --project tools/TimeForCode.Seeder`.
5. Run the other requests. Requests 2, 3 and 6 are anonymous.

Each API also serves its OpenAPI document (for example `http://localhost:8083/openapi/v1.json`), which Postman can import directly.

## Full collection: every endpoint of the three APIs

`TimeForCode-all-endpoints.postman_collection.json` holds all 60 operations of the Authorization, Donation and Reporting APIs, one folder per API and per tag, each request named `METHOD /path`. It is converted from the OpenAPI documents the API projects commit (`src/<Module>/TimeForCode.<Module>.Api/TimeForCode.<Module>.Api.json`), not written by hand, so it covers what the contract documents and nothing else.

1. Import the file and fill the collection variables: `adminSecret` (your `TEST_ADMIN_ACCESS_SECRET`), and the ids you need (`organizationId`, `projectId`, `contributorId`, `donationId`, `contributionId`, `userId`, `githubOwner`, `githubRepository`). The base URLs (`authorizationUrl`, `donationUrl`, `reportingUrl`) default to the local ports.
2. Run `Setup` first. That request is the one thing not taken from an OpenAPI document, because the test-only admin endpoint is hidden from it (ADR-017). It stores the admin token in `token`.
3. Every other request inherits bearer authentication with `{{token}}`. Anonymous endpoints ignore it. Endpoints that use the login cookies (`/api/v1/Authentication/...`) need a browser sign-in; Postman keeps the cookies it receives.
4. Body fields and unbound parameters hold placeholders (`<string>`, `1`); replace them before sending.

The sample responses are left out: the OpenAPI documents describe them. To refresh the collection after an API change, build the solution (it rewrites the OpenAPI documents) and run:

```powershell
pwsh scripts/generate-postman-collection.ps1
```

The script needs Node and fetches the pinned `openapi-to-postmanv2` converter through `npx`.
