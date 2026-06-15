---
mode: ask
description: Focuses a pull request review on the areas of highest risk and significance. Produces a prioritised review checklist tailored to the type of change in the PR.
---

# PR Review Focus Prompt

You are helping focus a pull request review for the **TimeForCode** project.

The pull request being reviewed is:

> **PR title:** {{pr_title}}
> **PR description:** {{pr_description}}
> **Changed files:** {{changed_files}}

---

## Your Task

Analyse the PR description and changed files, then produce a **prioritised review checklist** that focuses reviewer attention on the areas of highest risk. Do not reproduce generic coding advice — tailor every item to the specific change.

---

## Classification

First, classify the PR by its primary change type:

| Change type | Indicators |
|-------------|-----------|
| New feature | New controller actions, new handlers, new domain types |
| Bug fix | Changes to existing handler or validator logic |
| Refactor | No new behaviour, structural changes only |
| Infrastructure | Changes to `.bicep`, `.yaml`, `docker-compose`, `appsettings` |
| Documentation | Changes to `.md` files only |
| Dependency update | Changes to `.csproj` or `package.json` |
| Security fix | Changes related to auth, credentials, or vulnerability advisories |

---

## Review Focus Areas by Change Type

### New feature

- [ ] All new public methods have at least one positive-path and one negative-path unit test
- [ ] New handler returns `Result<T>.Failure("message")` on error — not a thrown exception
- [ ] New API endpoint has `[Authorize]` or `[AllowAnonymous]` attribute
- [ ] New endpoint appears in `docs/current/api-surface.md`
- [ ] Acceptance criteria from the linked issue are all covered by Reqnroll scenarios
- [ ] No `TODO(review):` markers are left unacknowledged in the implementation log

### Bug fix

- [ ] A test reproduces the bug before the fix (regression test)
- [ ] The fix is scoped to the reported issue — no unrelated changes
- [ ] Error handling follows project conventions (`Result<T>`, `ProblemDetails`)

### Refactor

- [ ] All existing tests still pass with zero modifications
- [ ] No behaviour change is introduced (verify with diff review)
- [ ] Architecture test coverage is unaffected

### Infrastructure

- [ ] No secrets or credentials are hardcoded in config files
- [ ] Environment variable names match those documented in `docs/current/deployment-status.md`
- [ ] Changes are backward compatible with the current deployed environment

### Documentation

- [ ] Markdown passes `markdownlint` with zero violations
- [ ] No current-state documents describe functionality that does not yet exist in code
- [ ] Internal links are valid

### Dependency update

- [ ] No `high` or `critical` CVEs are introduced
- [ ] Build and tests pass after the update
- [ ] No major-version updates are included without a noted justification

### Security fix

- [ ] The vulnerability is fully addressed — not just suppressed
- [ ] No new attack surface is introduced by the fix
- [ ] Security tests cover the fixed vulnerability path

---

## Output

Produce a focused review checklist for this specific PR:

```markdown
## PR Review Focus — {{pr_title}}

**Change type:** <classified type>
**Risk level:** Low / Medium / High

### Must verify

- [ ] <highest-priority item specific to this PR>
- [ ] <second-highest-priority item>

### Should verify

- [ ] <medium-priority item>
- [ ] <medium-priority item>

### If time permits

- [ ] <low-priority item>

### Questions for the author

1. <specific question about a non-obvious design decision in this PR>
2. <specific question about test coverage or error handling>
```

Tailor every item to the actual changes in this PR. Do not include checklist items that are irrelevant to the change type.

---

## Inventory Metadata

| Field | Value |
|-------|-------|
| Owner | `team` |
| Status | `active` |
| Overlap risk | `none` |
| Review cadence | `on-change` |
