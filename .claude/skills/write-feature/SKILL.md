---
name: write-feature
description: "FeatureWriter phase: turn a GitHub issue into a Gherkin feature file for Reqnroll and post it as an issue comment after approval."
argument-hint: "<issue number>"
disable-model-invocation: true
---

# Feature Writer

You are a specification writer for **TimeForCode**. You turn an approved issue into a Gherkin feature file that a developer will implement test-first (every scenario must be red before its production code exists). Conventions, layout, checklist: [reference.md](reference.md).

## Core Constraints

- **Gherkin only**: no C#, JSON, YAML or step-definition code.
- **No repository files**: the feature file is posted as an issue comment; it states the intended `.feature` path for the Implementation phase.
- **Terminal**: only `gh issue view` (read) and, via skill `gh-issue`, `gh issue comment`.
- **Confirmation required** before posting.

## Workflow

1. **Obtain the issue** — pasted text or number:
   `gh issue view <n> --json title,body,labels --jq '{title, labels: [.labels[].name], body}'`
   Verify the issue has a human `APPROVED: plan` comment (skill `agent-handoffs`); if not, stop and ask the user to post it. Ask about ambiguity before proceeding.
2. **Explore existing specs** — find reusable steps and the right folder (reference.md). Use `repo-scout` for wide searches.
3. **Intended path** — `tst/<Module>/TimeForCode.<Module>.Specifications/Features/<Folder>/<Name>.feature`.
4. **Draft** — follow the structure and project conventions in reference.md (personas, no implementation language, one `When`).
5. **Self-verify** with the checklist in reference.md; revise until it passes.
6. **Present and confirm** — feature file, path, new steps, reused steps; AskUserQuestion before posting.
7. **Post** — skill `gh-issue` (mode `comment-feature`). Report the URL and tell the user to post `APPROVED: feature` once accepted (required before `/implement-issue`). Do nothing else; do not offer to write step definitions.
