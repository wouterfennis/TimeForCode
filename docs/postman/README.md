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

## Every endpoint: import the OpenAPI documents

The API projects commit their OpenAPI documents, which describe every endpoint and stay current when the solution is built. In Postman choose Import and add the three files, which gives one collection per API:

- `src/Authorization/TimeForCode.Authorization.Api/TimeForCode.Authorization.Api.json`
- `src/Donation/TimeForCode.Donation.Api/TimeForCode.Donation.Api.json`
- `src/Reporting/TimeForCode.Reporting.Api/TimeForCode.Reporting.Api.json`

The documents declare no server, so set the `baseUrl` variable of each imported collection to its local address (Authorization `http://localhost:8080`, Donation `http://localhost:8082`, Reporting `http://localhost:8083`) and use a bearer token from request 1 above. The test-only admin token endpoint is hidden from the OpenAPI documents (ADR-017), so only the collection in this folder has it.
