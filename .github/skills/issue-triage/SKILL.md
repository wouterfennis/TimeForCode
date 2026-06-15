---
name: issue-triage
description: Reviews newly opened GitHub issues, validates completeness against the issue template, assigns labels, and posts a structured triage comment. Use when an issue lacks labels, has an incomplete body, or needs an initial quality assessment before planning work.
---

# Issue Triage Skill

This skill validates a GitHub issue against the project's quality standards and produces a triage decision. Follow every step in order.

---

## Inventory Metadata

| Field | Value |
|-------|-------|
| Owner | `Plan` |
| Status | `active` |
| Overlap risk | `none` |
| Review cadence | `on-change` |

---

## Trigger Conditions

Invoke this skill when:

- A new issue is opened and has no labels
- An issue body is suspected to be incomplete
- The Orchestrator or Plan agent requests an initial quality assessment

---

## Required Inputs

| Input | Source |
|-------|--------|
| Issue number | Provided by the calling agent |
| Authenticated `gh` CLI | Verified before running any command |

---

## Expected Outputs

- A triage comment posted on the issue
- One or more labels applied to the issue
- A clear `ACCEPT` or `NEEDS_WORK` decision returned to the calling agent

---

## Step 1 — Fetch the Issue

```powershell
gh issue view <number> --json number,title,body,labels,author,createdAt
```

Store the result. If the command fails, stop and report:
> "Could not fetch issue #`<number>`. Verify authentication and issue number."

---

## Step 2 — Validate the Title

Check that the title matches the format `[Type]: Short description`:

- Allowed types: `Feature`, `Bug`, `Improvement`, `Technical Debt`, `Documentation`
- Title must be between 10 and 100 characters

If the title is invalid, set flag `TITLE_INVALID=true`.

---

## Step 3 — Validate the Issue Body

Check that the issue body contains each of the following required sections:

| Required section | Pass condition |
|-----------------|----------------|
| `## Motivation / Context` | Section is present and non-empty |
| `## Proposed Solution` | Section is present and non-empty |
| `## Affected Areas` | Section is present and lists at least one item |
| `## Acceptance Criteria` | Section is present and contains at least one `- [ ]` checkbox |

For each missing or empty section, record it in `MISSING_SECTIONS`.

---

## Step 4 — Assign Labels

Determine the correct label(s) based on the issue type and content:

```powershell
gh label list
```

Apply label mapping:

| Issue type in title | Label |
|--------------------|-------|
| `Feature` | `enhancement` |
| `Bug` | `bug` |
| `Improvement` | `enhancement` |
| `Technical Debt` | `technical-debt` |
| `Documentation` | `documentation` |

Apply labels:

```powershell
gh issue edit <number> --add-label "<label>"
```

Only apply labels that exist in the repository. Skip any label that does not exist.

---

## Step 5 — Post Triage Comment

Compose the triage comment:

```powershell
$comment = @"
## 🏷️ Issue Triage

**Triage decision:** [ACCEPT | NEEDS_WORK]

### Checklist

| Check | Status |
|-------|--------|
| Title format (`[Type]: description`) | [✅ Pass | ❌ Fail] |
| Motivation / Context section | [✅ Pass | ❌ Missing or empty] |
| Proposed Solution section | [✅ Pass | ❌ Missing or empty] |
| Affected Areas section | [✅ Pass | ❌ Missing or empty] |
| Acceptance Criteria (at least one checkbox) | [✅ Pass | ❌ Missing or empty] |

[If NEEDS_WORK:]
### Action Required

Please update the following sections before this issue can be planned:

- [list missing sections]

[If ACCEPT:]
### Next Steps

This issue is ready for planning. The Plan agent can begin codebase exploration.
"@
gh issue comment <number> --body $comment
```

---

## Step 6 — Return Decision

Return one of the following to the calling agent:

- `ACCEPT` — all validation checks pass; issue is ready for the Plan agent
- `NEEDS_WORK` — one or more checks failed; issue author must update before proceeding

---

## Error Handling

| Symptom | Action |
|---------|--------|
| `gh: command not found` | Stop and instruct user to install `gh` CLI |
| Issue not found (404) | Stop and report the issue number may be wrong |
| Label does not exist | Skip that label; do not fail |
| Network timeout | Retry once; if it fails again, stop and report |
