---
name: issue-draft-templates
description: 'Single, parent and child GitHub issue body templates plus gh submission snippets for the Plan agent. Use when: drafting or submitting planned-work issues.'
---

# issue-draft-templates

Draft structures and `gh` submission snippets for Plan agent issues (single, parent, child).

### Step 4 — Draft the GitHub Issue(s)

Compose the draft(s) using the structure below. Every issue body must follow the same structure as the `.github/ISSUE_TEMPLATE/planned-work.yml` template.

**Rules for every draft (single, parent, or child):**

- Title format: `[Type]: Short, actionable description` (e.g., `[Feature]: Add donation expiry notification`)
- Motivation explains the *why* without assuming context
- Affected Areas references only verified files/components from Step 2
- Acceptance Criteria are specific, measurable, and independently testable
- No implementation code, pseudocode, or technical solution blueprints

**Single-issue draft structure (used when Step 3 did not split the work):**

```
**Title:** [Type]: Short description

**Labels:** label1, label2

---

## Motivation / Context

Why is this work needed? What problem does it solve or what value does it add?

## Proposed Solution

A high-level description of the intended approach. No code.

## Affected Areas

List the verified components and files from your codebase exploration:
- `src/Authorization/...` — reason
- `src/Donation/...` — reason

## Acceptance Criteria

- [ ] Specific, testable criterion 1
- [ ] Specific, testable criterion 2
- [ ] Specific, testable criterion 3

## Additional Context

Links to relevant docs, related issues, architecture diagrams, or other context.
```

**Parent issue draft structure (used when Step 3 split the work):**

```
**Title:** [Type]: Short description of the overall outcome

**Labels:** label1, label2, epic

---

## Motivation / Context

Why is this work needed, at the level of the overall outcome? What does "done" mean once every child issue is complete?

## Proposed Solution

A high-level description of the overall approach and how the pieces below fit together. No code.

## Affected Areas

List every verified component or file touched by any child.

## Child Issues

- [ ] #<child-1-number> — <child 1 title>
- [ ] #<child-2-number> — <child 2 title>

(Numbers are filled in during Step 7, after children are created.)

## Acceptance Criteria

- [ ] All child issues are complete
- [ ] <any overall criterion that only makes sense at the parent level, e.g. an end-to-end scenario spanning multiple children>

## Additional Context

Links to relevant docs, related issues, architecture diagrams, or other context.
```

**Child issue draft structure:**

```
**Title:** [Type]: Short description of this slice

**Labels:** label1, label2, agent-phase:feature-writer OR agent-phase:implementation-only, (agent-phase:skip-markdown-lint if applicable)

---

## Motivation / Context

Why is this slice needed, and how does it relate to the parent? ("Part of #<parent-number>: <parent title>.")

## Proposed Solution

A high-level description of the intended approach for this slice only. No code.

## Affected Areas

List only the verified components and files relevant to this slice.

## Acceptance Criteria

- [ ] Specific, testable criterion 1
- [ ] Specific, testable criterion 2

## Additional Context

Reference the parent issue and any sibling children this slice depends on or blocks.
```

### Step 7 — Submit via GitHub CLI

Once the user approves, use the `gh-issue-create` skill to submit via the GitHub CLI.

**Check authentication:**

```powershell
gh auth status
```

If not authenticated, instruct the user to run `gh auth login` and do not proceed until authentication is confirmed.

**Check available labels:**

```powershell
gh label list
```

Use only labels that exist. Map issue types to labels (e.g., Feature → `enhancement`, Bug Fix → `bug`). If a required `agent-phase:*` label does not yet exist in the repository, ask the user for permission to create it with `gh label create` before continuing.

**Single-issue submission (no split):**

```powershell
$issueBody = @"
## Motivation / Context

[full motivation text]

## Proposed Solution

[full proposed solution text]

## Affected Areas

[full affected areas list]

## Acceptance Criteria

- [ ] [criterion 1]
- [ ] [criterion 2]

## Additional Context

[full additional context]
"@

gh issue create `
  --title "[Type]: Short description" `
  --body $issueBody `
  --label "label1" `
  --label "label2"
```

**Parent/child submission (split work):**

Create every child first, then the parent, so the parent's task list can reference real issue numbers.

```powershell
# 1. Create each child issue, capturing its number from the output URL
$child1Body = @"
## Motivation / Context

Part of the upcoming parent issue: [parent title]

[rest of child 1 body]
"@

$child1Url = gh issue create `
  --title "[Type]: Child 1 title" `
  --body $child1Body `
  --label "label1" `
  --label "agent-phase:feature-writer"

# repeat for each additional child...

# 2. Create the parent, with a task list referencing the real child numbers
$parentBody = @"
## Motivation / Context

[full parent motivation text]

## Proposed Solution

[full parent proposed solution text]

## Affected Areas

[full affected areas list]

## Child Issues

- [ ] #<child-1-number> — Child 1 title
- [ ] #<child-2-number> — Child 2 title

## Acceptance Criteria

- [ ] All child issues are complete

## Additional Context

[full additional context]
"@

$parentUrl = gh issue create `
  --title "[Type]: Parent title" `
  --body $parentBody `
  --label "label1" `
  --label "epic"

# 3. Add a back-reference on each child pointing at the confirmed parent number
gh issue comment <child-1-number> --body "Part of #<parent-number>"
# repeat for each additional child...
```

If this repository's plan tier supports GitHub's native sub-issue relationship, prefer linking children as true sub-issues of the parent instead of (or in addition to) the task-list checkboxes above, so progress is reflected in GitHub's own UI.

**Confirm success:**
Report every created issue's URL to the user — the parent's first, then each child in the order they were drafted.

