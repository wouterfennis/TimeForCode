---
name: swagger-snapshot-review
description: "Checklist before accepting a changed Swagger verified snapshot (VerifyException from SwaggerTests). Use when: SwaggerTests fails."
allowed-tools: Read, Grep, Glob
---

# swagger-snapshot-review

## Swagger snapshot failures require a deliberate review

If a test in `TimeForCode.<Module>.Api.Tests` fails with a `VerifyException` from `SwaggerTests`, this is **not** an ordinary test failure. It is a deliberate API contract gate. Do not accept the snapshot update automatically.

Work through every item in this checklist before touching the snapshot file:

- [ ] **Intentionality** — Is the API surface change a direct, intended consequence of the work in this issue? Or is it an unintended side-effect of an internal refactor?
- [ ] **Breaking change** — Does the diff remove, rename, or change the type of an existing field or endpoint? If yes, existing callers are broken.
- [ ] **API versioning** — If the change is breaking, should a new API version (e.g. `/v2/`) be introduced instead of modifying the existing contract?
- [ ] **Optional fields** — If new fields are added, are they marked optional (`nullable: true` in the spec) so existing clients that do not send them are not broken?
- [ ] **NSwag client impact** — Does the `src/Authorization/TimeForCode.Authorization.Api.Client/` generated client need review? NSwag renames generated methods when endpoints are added or reordered (e.g. `RepositoriesAsync` → `RepositoriesAllAsync`). Check all callers of the affected methods.

**If any checklist item reveals a breaking change or unresolved question:**
Stop. Do not update the snapshot. Log the finding as a loose end and surface it to the user via AskUserQuestion before proceeding.

**Only when every item passes:**
Accept the snapshot by copying the received file over the verified file:

```powershell
Copy-Item `
  tst/<Module>/TimeForCode.<Module>.Api.Tests/SwaggerTests.Verify_GeneratedSwaggerFile_ShouldNotChangeUnlessIntended.received.txt `
  tst/<Module>/TimeForCode.<Module>.Api.Tests/SwaggerTests.Verify_GeneratedSwaggerFile_ShouldNotChangeUnlessIntended.verified.txt `
  -Force
```

Record the snapshot acceptance and the completed checklist as a completed item in the implementation log (Step 8).
