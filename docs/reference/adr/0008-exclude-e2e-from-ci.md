# ADR-008 — Exclude E2E tests from CI

**Date**: 2026-06-21
**Status**: Accepted

## Context

The `TimeForCode.Website.Specifications` project contains browser-level E2E tests powered by Microsoft Playwright. These tests require the full Docker Compose stack to be running (Website, Authorization API, Donation API, Identity Provider Mock, MongoDB). Running the stack in CI requires container-in-container support and significantly increases build times.

## Decision

All scenarios in `TimeForCode.Website.Specifications` are tagged `@E2E`. The CI pipeline passes `--filter "TestCategory!=E2E"` to `dotnet test`, so E2E tests are **never executed during automated builds**.

E2E tests are run manually against a locally started stack before significant releases or when changes affect the Website or the cross-service authentication flow.

**Manual run procedure:**

```powershell
# 1. Start the full stack
.\scripts\start-local.ps1

# 2. Install Playwright browser binaries (first time only)
pwsh tst/Website/TimeForCode.Website.Specifications/bin/Debug/net10.0/playwright.ps1 install chromium

# 3. Run the E2E suite
dotnet test tst/Website/TimeForCode.Website.Specifications/
```

## Consequences

**Positive**: CI remains fast. E2E failures do not block unrelated pull requests. Developers without a full local stack are not blocked.

**Negative**: E2E regressions may go undetected between manual runs. The CI suite does not catch issues that only appear at the full-stack integration level (e.g. cross-service cookie handling, OAuth redirect flows).
