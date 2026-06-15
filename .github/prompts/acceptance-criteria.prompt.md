---
mode: ask
description: Helps a contributor write high-quality, independently testable acceptance criteria for a GitHub issue or feature request. Produces criteria that map cleanly to Gherkin scenarios.
---

# Acceptance Criteria Prompt

You are helping write **acceptance criteria** for a feature or issue in the **TimeForCode** project.

The feature being described is:

> **Feature title:** {{feature_title}}
> **Summary:** {{feature_summary}}

---

## Your Task

Guide the contributor through writing acceptance criteria that are:

- **Specific** — describe a concrete observable outcome, not a vague goal
- **Measurable** — a developer can unambiguously verify whether the criterion is met
- **Independent** — each criterion can be tested in isolation
- **Behaviour-focused** — describe what the system does, not how it does it
- **Gherkin-ready** — each criterion should translate directly to one or more Gherkin scenarios

---

## Questions to Ask

Ask the contributor these questions to derive the criteria:

1. **Happy path**: What is the most common successful outcome a user expects from this feature?
2. **Edge cases**: What inputs or conditions might cause the feature to behave differently? (empty values, duplicates, missing data)
3. **Error cases**: What should happen when something goes wrong? (invalid input, service unavailable, unauthorised access)
4. **Constraints**: Are there any business rules that must always hold? (e.g., "a project can only be registered once", "status must be Published after creation")
5. **Security**: Does this feature involve authentication, authorisation, or data privacy? If so, what access rules must be enforced?

---

## Acceptance Criteria Format

Write each criterion as a checkbox in this form:

```text
- [ ] Given <context>, when <action>, then <expected outcome>
```

Or for simpler criteria:

```text
- [ ] <Observable behaviour or system state>
```

---

## Quality Checklist

Before finalising, verify each criterion against this checklist:

| Check | Pass condition |
|-------|---------------|
| No implementation detail | Criterion does not name a class, method, HTTP verb, or status code |
| Independently testable | Criterion can be verified without relying on another criterion being true first |
| Unambiguous | Two developers reading the criterion would reach the same conclusion about whether it is met |
| Maps to a scenario | The criterion can be expressed as a Gherkin `Scenario:` with Given/When/Then |

---

## Output

Produce a final acceptance criteria list in GitHub checkbox format:

```markdown
## Acceptance Criteria

- [ ] Given a registered user, when they submit a valid project registration, then the project is created with status "Published".
- [ ] Given an unauthenticated request, when project registration is attempted, then the request is rejected with an authorisation error.
- [ ] Given a project with the same identifier already exists, when registration is attempted again, then the request is rejected with a conflict error.
- [ ] Given a project is successfully registered, then it appears in the project list.
```

Ask the contributor to confirm the list before it is used.
