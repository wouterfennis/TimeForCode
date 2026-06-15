---
name: changelog-update
description: Reads merged pull requests since the last release tag, generates a changelog entry for the target version, and prepends it to CHANGELOG.md. Use before tagging a release or when the release-readiness skill requests changelog verification.
---

# Changelog Update Skill

This skill generates and prepends a changelog entry for a new release version. Follow every step in order.

---

## Inventory Metadata

| Field | Value |
|-------|-------|
| Owner | `release-manager` |
| Status | `active` |
| Overlap risk | `release-readiness` (release-readiness calls this skill in verification mode) |
| Review cadence | `per-release` |

---

## Trigger Conditions

Invoke this skill when:

- The Release Manager agent is preparing a new release
- The `release-readiness` skill reports `CHANGELOG_MISSING`
- A team member requests a changelog update before tagging

---

## Required Inputs

| Input | Source |
|-------|--------|
| Target version string (e.g., `1.2.0`) | Provided by the calling agent or user |
| Previous release tag (e.g., `v1.1.0`) | Provided by the calling agent, or derived in Step 1 |
| Authenticated `gh` CLI | Verified before running any command |

---

## Expected Outputs

- A new version entry prepended to `CHANGELOG.md`
- Confirmation returned to the calling agent: `CHANGELOG_UPDATED` or `ALREADY_UP_TO_DATE`

---

## Changelog Format

`CHANGELOG.md` uses [Keep a Changelog](https://keepachangelog.com/en/1.0.0/) format:

```markdown
## [<version>] — <YYYY-MM-DD>

### Added
- …

### Changed
- …

### Fixed
- …

### Removed
- …

### Security
- …
```

Omit sections that have no entries.

---

## Step 1 — Find the Previous Release Tag

If the previous release tag was not provided, find it:

```powershell
gh release list --limit 1 --json tagName --jq '.[0].tagName'
```

If no releases exist, use the repository's first commit as the baseline.

---

## Step 2 — List Merged Pull Requests Since Last Tag

```powershell
gh pr list --state merged --base main --json number,title,labels,mergedAt --limit 100 | `
  ConvertFrom-Json | `
  Where-Object { $_.mergedAt -gt "<last_tag_date>" } | `
  Select-Object number, title, labels
```

For each PR, determine its changelog category based on labels:

| Label | Changelog section |
|-------|------------------|
| `enhancement` | Added |
| `bug` | Fixed |
| `technical-debt` | Changed |
| `documentation` | Changed |
| `security` | Security |
| `breaking-change` | Changed (note as breaking) |
| (no matching label) | Changed |

---

## Step 3 — Verify CHANGELOG.md Exists

```powershell
Test-Path CHANGELOG.md
```

If `CHANGELOG.md` does not exist, create it with a standard header:

```markdown
# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).
```

---

## Step 4 — Check Whether Entry Already Exists

Search for the target version in `CHANGELOG.md`:

```powershell
Select-String -Path CHANGELOG.md -Pattern "\[$target_version\]"
```

If found, report `ALREADY_UP_TO_DATE` and stop. Do not modify the file.

---

## Step 5 — Build the New Entry

Compose the new changelog entry using the PR data from Step 2:

```markdown
## [<version>] — <YYYY-MM-DD>

### Added
- PR #N: <title>

### Fixed
- PR #N: <title>

### Changed
- PR #N: <title>
```

Entries within each section are sorted by PR number ascending.

---

## Step 6 — Prepend Entry to CHANGELOG.md

1. Read the current `CHANGELOG.md` content
2. Insert the new entry immediately after the header block (the first blank line after the introductory paragraphs)
3. Write the updated content back to `CHANGELOG.md`

---

## Step 7 — Present for Approval

Present the new entry to the calling agent or user and ask for approval before committing the file.

If approved, the calling agent is responsible for staging and committing the change.

---

## Error Handling

| Symptom | Action |
|---------|--------|
| `gh` not authenticated | Stop and instruct user to run `gh auth login` |
| No merged PRs found | Create an entry with a single line: `- No notable changes` |
| `CHANGELOG.md` is not writable | Stop and report permission error |
| Target version already in changelog | Report `ALREADY_UP_TO_DATE` and stop |
