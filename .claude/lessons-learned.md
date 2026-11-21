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

### Two sign-in-heavy runs within a minute can still hit the `auth` limit

- **Date / issue:** 2026-11-14 / #194, updated 2026-11-20 / #190
- **Lesson:** The sign-in, token, refresh and callback endpoints of the Authorization API share one `auth` rate limit, a single bucket for everyone (40 requests a minute since the limits were doubled in #190, was 20). One smoke run uses most of the old budget; the seeder (eight personas) alone exceeded 20 and failed with `429` at `anonymised-contributor`. The CI wait step between the Donation and Reporting live scenarios was removed once smoke, Donation live and Reporting live passed back to back at 40.
- **How to apply:** Check for `429` before debugging a sign-in failure. Running the smoke test and the seeder in the same minute can still exceed 40; leave a minute between them.

### `podman compose` fails or hangs when the Docker API pipe is missing

- **Date / issue:** 2026-11-17 / #205
- **Lesson:** `podman compose` delegates to `docker-compose.exe`, which needs the named pipe `\\.\pipe\podman-machine-default`. When `win-sshproxy.exe` fails to start ("API forwarding for Docker API clients is not available"), compose fails with "failed to connect to the docker API". Pointing `DOCKER_HOST` at the machine's ssh socket then hangs silently: ssh waits for a host-key prompt and the machine key is not in the agent.
- **How to apply:** Do not restart the Podman machine to fix it: that stops the whole stack. Accept the host key once (`ssh -o StrictHostKeyChecking=accept-new -i ~/.local/share/containers/podman/machine/machine -p <port> root@127.0.0.1 true`), `ssh-add` the machine key, set `DOCKER_HOST=ssh://root@127.0.0.1:<port>/run/podman/podman.sock` (port from `podman system connection list`), and run compose in the background with output to a file so a silent hang is visible. A hook blocks any command containing the word "docker", so keep it out of waits and greps. Ask before restarting the machine or interrupting a long compose build.


### Use `///` XML docs above declarations; `//` above one fails the build

- **Date / issue:** 2026-11-21 / #240
- **Lesson:** A `//` comment directly above a type or member fails the build (analyzer `TFC0001`, `src/Shared/TimeForCode.Shared.Analyzers`, wired in through `Directory.Build.props`). Use `/// <summary>`; `//` stays for bodies, `TODO` markers and comments separated by a blank line. Rule text: `.claude/rules/code-style.md`.
- **Why:** `//` comments do not show in IDE tooltips or generated docs, and the code base mixed both styles. Without `GenerateDocumentationFile` Roslyn lexes `///` lines as plain single-line comments, so a syntax-based check must tell them apart by text (`///` but not `////`); a test that parses with default options hides this.
- **How to apply:** When the analyzer fails a build, convert the comment to an XML doc (escape `<`, `>`, `&`); never suppress the rule. Parse analyzer test samples with `DocumentationMode.None`.
