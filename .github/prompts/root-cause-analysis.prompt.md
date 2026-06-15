---
mode: ask
description: Guides a structured root cause analysis for a bug, test failure, or production incident in the TimeForCode project. Produces a five-why chain, a timeline, and a remediation plan.
---

# Root Cause Analysis Prompt

You are facilitating a **root cause analysis (RCA)** for an issue in the **TimeForCode** project.

The issue being analysed is:

> **Issue title:** {{issue_title}}
> **Symptom:** {{symptom_description}}
> **Environment:** {{environment}} (e.g., local, staging, production)
> **First observed:** {{first_observed_date}}

---

## Your Task

Guide the contributor through a structured RCA using the five-why technique. Produce a timeline, a root cause chain, and a remediation plan that prevents recurrence.

---

## Step 1 — Confirm the Symptom

Before analysing causes, confirm the symptom is precisely described.

Ask:
> "What is the exact observable failure? What did you expect to happen, and what happened instead?"

The symptom must be stated as: "When [action], [component] [actual outcome] instead of [expected outcome]."

---

## Step 2 — Build the Timeline

Ask the contributor to reconstruct the sequence of events:

> "Walk me through what happened in order. Include:
>
> - When the symptom was first noticed
> - Any recent deployments, merges, or configuration changes
> - Any error messages or log entries
> - Which components or services were involved"

Produce a timeline table:

| Time | Event |
|------|-------|
| … | … |

---

## Step 3 — Five-Why Chain

Starting from the confirmed symptom, apply the five-why technique:

**Why 1:** Why did [symptom] occur?
→ [Immediate cause]

**Why 2:** Why did [immediate cause] occur?
→ [Contributing cause]

**Why 3:** Why did [contributing cause] occur?
→ [Deeper cause]

**Why 4:** Why did [deeper cause] occur?
→ [Systemic cause]

**Why 5:** Why did [systemic cause] occur?
→ [Root cause]

Stop when you reach a cause that:

- Is within the team's control to fix
- Would prevent the symptom from recurring if addressed

---

## Step 4 — Classify the Root Cause

Classify the root cause into one of these categories:

| Category | Description |
|----------|-------------|
| Missing test | A test would have caught this before it reached production |
| Incomplete error handling | An error path was not handled or was silently swallowed |
| Documentation gap | A developer misunderstood intended behaviour due to missing or wrong docs |
| Dependency change | An external dependency changed behaviour unexpectedly |
| Configuration drift | A configuration value differed between environments |
| Design flaw | The architecture made this class of failure likely |
| Human error | A one-time mistake not caused by systemic gaps |

---

## Step 5 — Remediation Plan

For the identified root cause, produce a remediation plan:

### Immediate fix

> What is the minimum change needed to restore correct behaviour right now?

### Recurrence prevention

> What systemic change prevents this class of issue from recurring?

This could be:

- A new test (specify what it should verify)
- An updated error handling path
- A documentation update
- An architecture change (log as a GitHub issue)
- A process change (e.g., add to pre-release checklist)

---

## Output

Produce the final RCA document:

```markdown
## Root Cause Analysis

**Issue:** {{issue_title}}
**Date:** <ISO date>
**Author:** <contributor name>

---

### Symptom

When [action], [component] [actual outcome] instead of [expected outcome].

---

### Timeline

| Time | Event |
|------|-------|
| … | … |

---

### Five-Why Chain

| Why | Cause |
|-----|-------|
| 1 | … |
| 2 | … |
| 3 | … |
| 4 | … |
| 5 (Root) | … |

---

### Root Cause Category

<category name> — <one-sentence explanation>

---

### Remediation

**Immediate fix:** <description>

**Recurrence prevention:** <description>

**Tracking:** <link to GitHub issue or PR if one was created>
```
