# Lessons learned

Non-obvious knowledge from past sessions that has no better home. Read and update through skill `lessons-learned`; entry format is defined there.

Before adding an entry, check whether it belongs in a rule (`.claude/rules/`), a skill, a hook (`.claude/hooks/`), a script (`scripts/`) or a code comment instead; those load at the right moment, this file does not. Past lessons were moved there:

- Specs (`DonationContext`, `SessionContext`, catalog fakes, `@live` login), assertions library: `.claude/rules/testing-strategy.md`
- Input limits, token scopes: skill `dotnet-conventions`
- NSwag client and Swagger snapshot: skill `swagger-snapshot-review`
- Solution-wide format churn: `.claude/hooks/format-solution.ps1`, skill `dotnet-format`
- Approval markers: `scripts/check-approval.ps1`, skill `agent-handoffs`
- Stale local Mongo volume: `scripts/start-local.ps1 -ResetData`
- `nginx -t` at image build: comment in `Dockerfile.frontend`
- Project status numbers: comment on the enum plus `ProjectStatusTests`

## Entries
