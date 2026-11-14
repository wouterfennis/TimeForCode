# Lessons learned

Non-obvious knowledge from past sessions that has no better home. Read and update through skill `lessons-learned`; entry format is defined there.

Before adding an entry, check whether it belongs in a rule (`.claude/rules/`), a skill, a hook (`.claude/hooks/`), a script (`scripts/`) or a code comment instead; those load at the right moment, this file does not. Past lessons were moved there:

- Specs (`DonationContext`, `SessionContext`, catalog fakes, `@live` login), assertions library: `.claude/rules/testing-strategy.md`
- Input limits, token scopes: skill `dotnet-conventions`
- NSwag client and Swagger snapshot: skill `swagger-snapshot-review`
- Solution-wide format churn: `.claude/hooks/format-solution.ps1`, skill `dotnet-format`
- Approval markers: `scripts/check-approval.ps1`, skill `agent-handoffs`
- Stale local Mongo volume: `scripts/start-local.ps1 -ResetData`
- Project status numbers: comment on the enum plus `ProjectStatusTests`

## Entries

### Keep `generate:api` on npx until openapi-typescript supports TypeScript 7

- **Date / issue:** 2026-10-27 / #112
- **Lesson:** `openapi-typescript` (latest 7.13.0) needs the TypeScript 5 JavaScript compiler API; the project's TypeScript 7 has none. An npm `overrides` entry fails because the root also depends on `typescript`, so `generate:api` runs through `npx` with pinned `openapi-typescript` and `typescript@5.9.3`.
- **Why:** As a devDependency it fails to install (ERESOLVE) or crashes with `createKeywordTypeNode` undefined. package.json cannot hold a comment, so the reason is easy to lose.
- **How to apply:** Before changing the script or the TypeScript version, check whether `openapi-typescript` now supports TS 7; if so, make it a devDependency. A separate tool package with its own lockfile was considered and declined. See `docs/frontend/architecture.md` (API client generation).

### Re-read files before rewriting them after a long gap

- **Date / issue:** 2026-10-27 / #112
- **Lesson:** Another session committed changes to files this session had written earlier (`donationClient.ts`, `useProjects.ts`). Rewriting them from memory overwrote that work in the working tree.
- **Why:** Only the uncommitted state was lost, restored with `git checkout`, but the rewrite started from a stale picture and a review finding was wrongly dismissed.
- **How to apply:** Before editing or writing a file that was touched earlier in a long session, run `git log --oneline -- <path>` and `git status`, and read the file; never overwrite from memory.

### Run vitest from a path with the exact drive-letter case

- **Date / issue:** 2026-11-14 / #194
- **Lesson:** From a shell whose working directory is `c:\Git\...` (lower-case drive letter), every vitest file fails with "Vitest failed to find the current suite" at `tests/setup.ts`, even the files nobody touched. Vitest loads a second copy of itself for the setup file.
- **Why:** The path casing differs between the config and the setup file on Windows, so the module is resolved twice.
- **How to apply:** Run `npm test` from `C:\Git\...` (upper-case drive letter), for example `Set-Location "C:\Git\TimeForCode\src\Frontend\TimeForCode.Frontend"` in the PowerShell tool, before concluding that the front-end suite is broken.

### The smoke test cannot run twice within a minute

- **Date / issue:** 2026-11-14 / #194
- **Lesson:** The sign-in, token, refresh and callback endpoints of the Authorization API share the `auth` rate limit (20 requests a minute). One smoke run uses most of it, so a second run (or a manual sign-in script) in the same minute fails at "Code exchange" with `429`, which looks like a regression.
- **How to apply:** Wait a minute between smoke runs and manual sign-in scripts; check for `429` before debugging.
