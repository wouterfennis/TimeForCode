---
paths:
  - "src/**/*.Domain/**"
  - "src/**/*.Values/**"
  - "src/**/*.Commands/**"
---

# Domain, Values and Commands projects

- These projects depend on nothing else in the solution (ArchUnitNET enforces this). Do not add project references or `using` directives pointing at Application, Infrastructure or API.
- Entities and value objects hold behaviour and invariants; no persistence attributes beyond what the existing `DocumentEntity` pattern uses, no I/O.
- Commands implement `IRequest<Result<T>>`; result types are plain records.
- Duplicating a small entity across bounded contexts is intentional (see arc42 chapter 09); do not reference another module's Domain.
- Any new rule or type here needs a red test first (`.claude/rules/testing-strategy.md`).
