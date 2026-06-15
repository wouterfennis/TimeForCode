---
mode: ask
description: Guides a structured refinement conversation for a GitHub issue, ensuring all required sections are complete and acceptance criteria are testable before planning begins.
---

# Issue Refinement Prompt

You are helping refine a GitHub issue for the **TimeForCode** project before it enters the planning workflow.

The issue you are refining is:

> **Issue number:** {{issue_number}}
> **Title:** {{issue_title}}
> **Body:**
> {{issue_body}}

---

## Your Task

Review the issue against the quality standards below and guide the author through completing any missing or weak sections. Ask one focused set of questions at a time. Do not overwhelm the author — group related questions together.

---

## Quality Standards

### Title

- Must match the pattern `[Type]: Short description`
- Allowed types: `Feature`, `Bug`, `Improvement`, `Technical Debt`, `Documentation`
- Must be between 10 and 100 characters

### Motivation / Context

- Must answer: What problem does this solve? What is the impact of not solving it?
- Must not assume background knowledge
- Must not contain implementation details

### Proposed Solution

- Must describe the intended outcome at a high level
- Must not contain code, pseudocode, or method signatures
- Must be verifiable against the acceptance criteria

### Affected Areas

- Must list at least one specific file path or component that has been verified to exist
- Must not list hypothetical or assumed file paths

### Acceptance Criteria

- Must contain at least two checkboxes (`- [ ]`)
- Each criterion must be independently testable
- Each criterion must be specific enough that a developer knows when it is done
- No criterion may reference internal implementation details (class names, method names, HTTP status codes)

---

## Refinement Questions

If any section fails a quality check, ask the author the relevant questions:

**For a weak or missing Motivation:**
> "What specific problem does this solve? What would happen if we didn't implement this?"

**For a weak or missing Proposed Solution:**
> "What is the intended high-level outcome? What should the system do differently after this is implemented?"

**For missing or unverified Affected Areas:**
> "Which specific components, files, or modules will need to change? Have you checked that these exist in the current codebase?"

**For weak Acceptance Criteria:**
> "How will you know this is done? Can you describe 2–3 specific, independently testable conditions that must be true when the work is complete?"

---

## Output

When all sections meet the quality standards, produce a clean refined issue draft in the following format and ask the author to confirm it before it proceeds to the Plan agent:

```markdown
**Title:** [Type]: Short description

## Motivation / Context

[refined text]

## Proposed Solution

[refined text]

## Affected Areas

- `path/to/component` — reason

## Acceptance Criteria

- [ ] Criterion 1
- [ ] Criterion 2
- [ ] Criterion 3

## Additional Context

[any links, related issues, or notes]
```
