---
name: dependency-update
description: Audits NuGet and npm dependencies in the TimeForCode solution for available updates and known vulnerabilities. Produces an update report and applies safe patch-level updates. Use during maintenance passes or before a release to reduce dependency risk.
---

# Dependency Update Skill

This skill audits dependencies, reports available updates, and applies safe patch-level updates. Follow every step in order.

---

## Inventory Metadata

| Field | Value |
|-------|-------|
| Owner | `maintenance` |
| Status | `active` |
| Overlap risk | `security-scan-report` (vulnerability findings are shared with that skill) |
| Review cadence | `monthly` |

---

## Trigger Conditions

Invoke this skill when:

- The Maintenance agent runs its monthly hygiene pass
- A `dependabot` alert is raised and manual triage is required
- Pre-release checks request a dependency health review

---

## Required Inputs

| Input | Source |
|-------|--------|
| Authenticated `dotnet` SDK | Verified before running any command |
| Repository root path | Working directory |

---

## Expected Outputs

- A dependency update report listing outdated and vulnerable packages
- Patch-level updates applied to project files (with human approval)
- A `CLEAN` or `ACTION_REQUIRED` decision returned to the calling agent

---

## Step 1 — Audit NuGet Packages for Vulnerabilities

```powershell
dotnet list package --vulnerable --include-transitive 2>&1
```

Record all packages flagged with a severity level (`low`, `moderate`, `high`, `critical`).

If any `high` or `critical` vulnerabilities are found, immediately set flag `CRITICAL_VULN=true`.

---

## Step 2 — List Outdated NuGet Packages

```powershell
dotnet list package --outdated 2>&1
```

For each outdated package, record:

- Package name
- Current version
- Latest stable version
- Whether this is a patch, minor, or major update

---

## Step 3 — Classify Updates

Classify each outdated package by risk level:

| Update type | Risk | Action |
|-------------|------|--------|
| Patch (x.y.**Z**) | Low | Apply automatically after human approval |
| Minor (x.**Y**.z) | Medium | Propose update; require human approval before applying |
| Major (**X**.y.z) | High | Propose update; require explicit human instruction |
| Vulnerable (any severity) | Varies | Treat as at least Medium regardless of version delta |

---

## Step 4 — Check npm Dependencies (if applicable)

Check for a `package.json` in the repository:

```powershell
Get-ChildItem -Recurse -Filter "package.json" -Path src | Where-Object { $_.FullName -notmatch "node_modules" }
```

If any are found, for each:

```powershell
npm audit --json 2>&1
```

Record:

- Total vulnerabilities by severity
- Package names and advisories

---

## Step 5 — Present Update Proposal

Before applying any changes, present the full proposal to the calling agent or user:

```markdown
## Dependency Update Proposal

### NuGet — Vulnerable Packages

| Package | Current | Severity | Advisory |
|---------|---------|----------|----------|
| Foo.Bar | 1.2.3 | high | CVE-XXXX-YYYY |

### NuGet — Available Updates

| Package | Current | Latest | Update type |
|---------|---------|--------|-------------|
| Baz.Qux | 2.0.1 | 2.0.4 | patch |
| …       | …       | …      | minor       |

### npm — Vulnerabilities

| Package | Severity | Advisory |
|---------|----------|----------|
| …       | …        | … |

**Proposed automatic changes (patch-level only):**
- Update Baz.Qux from 2.0.1 → 2.0.4

Do you approve applying the patch-level NuGet updates?
```

**Wait for explicit approval before applying any changes.**

---

## Step 6 — Apply Approved Updates

For each approved patch-level NuGet update:

```powershell
dotnet add <project_path> package <PackageName> --version <latest_version>
```

After all updates, run the build and tests to verify no regressions:

```powershell
dotnet build TimeForCode.sln --no-incremental 2>&1
dotnet test TimeForCode.sln --no-build 2>&1
```

If the build or tests fail after an update, revert that package update and record it as `UPDATE_BLOCKED`.

---

## Step 7 — Report Final Status

```markdown
## Dependency Update Report

**Date:** <ISO date>
**Decision:** CLEAN / ACTION_REQUIRED

| Category | Count |
|----------|-------|
| Critical/High vulnerabilities | N |
| Patch updates applied | N |
| Minor updates proposed (pending human action) | N |
| Major updates proposed (pending human action) | N |
| Updates blocked by regression | N |
```

Return `ACTION_REQUIRED` if any `high` or `critical` vulnerabilities remain unresolved.

---

## Error Handling

| Symptom | Action |
|---------|--------|
| `dotnet` not found | Stop and report .NET SDK is not installed |
| `npm` not found | Skip Step 4 and note npm is not installed |
| Build fails after update | Revert the specific package and mark `UPDATE_BLOCKED` |
| Network error fetching package info | Retry once; if it fails again, report the error and skip that package |
