# ADR-006 — Use Reqnroll + BDD for acceptance tests

**Date**: 2026-06-21
**Status**: Accepted

## Context

The project needed an acceptance-test approach that can be understood by non-developers (product owners, AI agents), stays coupled to the feature specification throughout the lifetime of the feature, and produces human-readable output in CI logs.

The main alternatives considered were:

- Plain xUnit/MSTest integration tests (code-only, no readable specification)
- Reqnroll / SpecFlow with Gherkin scenarios (code + specification in `.feature` files)
- Postman / Newman collections (JSON-based, hard to version alongside code)

## Decision

[Reqnroll](https://reqnroll.net/) (the community continuation of SpecFlow) is used as the BDD framework for all acceptance-test projects (`*.Specifications`). Feature files are written in Gherkin and committed alongside the step definitions and production code.

Step text follows a persona convention — subjects are "The user", "The external platform", or "The time for code platform" — to keep scenarios free of implementation detail.

## Consequences

**Positive**: Feature files serve as living documentation that is always in sync with the test suite. AI agents (feature-writer, implementation, review) can read and produce Gherkin unambiguously. Scenarios map directly to acceptance-criteria checkboxes on GitHub Issues.

**Negative**: Requires discipline to keep step definitions DRY. Gherkin can become verbose for data-heavy scenarios; `Scenario Outline` + `Examples` tables mitigate this.
