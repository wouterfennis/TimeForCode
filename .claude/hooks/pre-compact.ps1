# PreCompact hook: snapshots working state to .agent-state/handoff.md so a compacted or fresh session
# resumes from ~300 tokens. Keeps anything the agent wrote under "## Notes" (see context-handoff skill).
$ErrorActionPreference = 'SilentlyContinue'
$root = (git rev-parse --show-toplevel 2>$null)
if (-not $root) { exit 0 }
Set-Location $root

$dir = Join-Path $root '.agent-state'
$file = Join-Path $dir 'handoff.md'
New-Item -ItemType Directory -Force -Path $dir | Out-Null

$notes = ''
if (Test-Path $file) {
    $existing = Get-Content -LiteralPath $file -Raw
    if ($existing -match '(?s)(## Notes.*)$') { $notes = $Matches[1] }
}
if (-not $notes) { $notes = "## Notes`n(none yet - agent: add issue number, phase, done / remaining, open questions)`n" }

$branch = git branch --show-current
$stat = @(git diff --stat HEAD | Select-Object -Last 15) -join "`n"
$untracked = @(git ls-files --others --exclude-standard | Select-Object -First 15) -join "`n"

$body = @"
# Handoff snapshot ($(Get-Date -Format 's'))
Branch: $branch

## Changed files (git diff --stat HEAD)
$stat

## Untracked
$untracked

$notes
"@
Set-Content -LiteralPath $file -Value $body -Encoding utf8
exit 0
