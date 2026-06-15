---
name: security-scan-report
description: Runs a security scan of the TimeForCode codebase using dotnet vulnerability auditing and static analysis. Produces a structured security report grouped by severity. Use as part of release readiness checks or the maintenance agent's security pass.
---

# Security Scan Report Skill

This skill produces a security scan report for the **TimeForCode** repository. It does not modify any source files. Follow every step in order.

---

## Inventory Metadata

| Field | Value |
|-------|-------|
| Owner | `release-manager` |
| Status | `active` |
| Overlap risk | `dependency-update` (vulnerability data is shared with that skill) |
| Review cadence | `per-release` |

---

## Trigger Conditions

Invoke this skill when:

- The Release Manager agent runs pre-release security checks
- The Maintenance agent runs its security hygiene pass
- A security incident or advisory requires an immediate scan

---

## Required Inputs

| Input | Source |
|-------|--------|
| Authenticated `dotnet` SDK | Verified before running any command |
| Repository root path | Working directory |

---

## Expected Outputs

- A structured security report with findings grouped by severity
- A `SECURE` or `VULNERABILITIES_FOUND` decision returned to the calling agent

---

## Step 1 — Check NuGet Package Vulnerabilities

```powershell
dotnet list package --vulnerable --include-transitive 2>&1
```

For each vulnerable package, record:

- Package name
- Affected version
- Severity (`low`, `moderate`, `high`, `critical`)
- CVE or advisory ID (if available in the output)

---

## Step 2 — Scan for Hardcoded Secrets

Search for patterns that indicate hardcoded secrets or credentials:

```powershell
Get-ChildItem -Recurse -Include "*.cs","*.json","*.yaml","*.yml","*.env" -Path src | `
  Select-String -Pattern "password\s*=|secret\s*=|api[_-]?key\s*=|token\s*=|connectionstring\s*=" -CaseSensitive:$false | `
  Where-Object { $_.Line -notmatch "//.*|<!--.*-->|placeholder|example|your-" } | `
  Select-Object Path, LineNumber, Line
```

Record any matches as potential `HARDCODED_SECRET` findings. Each finding must be reviewed manually — the pattern match is not conclusive.

---

## Step 3 — Check for Insecure HTTP Usage

Search for `http://` references in production code (non-test, non-comment):

```powershell
Get-ChildItem -Recurse -Include "*.cs" -Path src | `
  Select-String -Pattern "http://" | `
  Where-Object { $_.Line -notmatch "//|localhost|127\.0\.0\.1" } | `
  Select-Object Path, LineNumber, Line
```

Record any matches as `INSECURE_HTTP`.

---

## Step 4 — Check for SQL Injection Patterns

Search for raw string concatenation in database query contexts:

```powershell
Get-ChildItem -Recurse -Include "*.cs" -Path src | `
  Select-String -Pattern "\"SELECT |\"INSERT |\"UPDATE |\"DELETE " | `
  Select-Object Path, LineNumber, Line
```

Record any raw SQL string literals that are not parameterised as `POTENTIAL_SQL_INJECTION`.

---

## Step 5 — Check Authorization Attributes

Verify that all controller endpoints have explicit authorization:

```powershell
Get-ChildItem -Recurse -Filter "*Controller.cs" -Path src | `
  Select-String -Pattern "\[Http(Get|Post|Put|Delete|Patch)\]" -Context 5,0 | `
  Where-Object { $_.Context.PreContext -notmatch "\[Authorize|\[AllowAnonymous" } | `
  Select-Object Path, LineNumber
```

Record any endpoint that has neither `[Authorize]` nor `[AllowAnonymous]` as `MISSING_AUTH_ATTRIBUTE`.

---

## Step 6 — Compose the Security Report

```markdown
## Security Scan Report

**Date:** <ISO date>
**Decision:** SECURE / VULNERABILITIES_FOUND

---

### 🔴 Critical / High Findings

| Type | Location | Detail |
|------|----------|--------|
| VULNERABLE_PACKAGE | Foo.Bar 1.2.3 | CVE-XXXX-YYYY (high) |
| HARDCODED_SECRET | src/…/appsettings.json:L12 | Matched pattern `****** |

---

### 🟡 Medium Findings

| Type | Location | Detail |
|------|----------|--------|
| INSECURE_HTTP | src/…/Client.cs:L34 | Non-localhost http:// reference |

---

### 🔵 Low / Informational Findings

| Type | Location | Detail |
|------|----------|--------|
| POTENTIAL_SQL_INJECTION | src/…/Repository.cs:L78 | Raw SQL string — verify parameterisation |

---

### Summary

| Severity | Count |
|----------|-------|
| Critical | N |
| High | N |
| Medium | N |
| Low | N |

[If SECURE:] ✅ No security findings detected.
```

---

## Error Handling

| Symptom | Action |
|---------|--------|
| `dotnet` not found | Stop and report .NET SDK is not installed |
| `Get-ChildItem` not available | Use `find` and `grep` as fallback |
| Pattern match produces false positive | Note it as "Requires manual review" in the report |
| Scan takes more than 5 minutes | Report timeout and list what was completed |

---

## Stop Conditions

Return `VULNERABILITIES_FOUND` and stop the calling agent's release flow if:

- Any `high` or `critical` NuGet vulnerability is found
- Any confirmed `HARDCODED_SECRET` is found

All other findings are reported but do not block the release automatically.
