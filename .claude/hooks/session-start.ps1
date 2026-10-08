# SessionStart hook: injects a ~20-line orientation block so agents do not rediscover it.
# Also re-injects the handoff note written by pre-compact.ps1 / the context-handoff skill.
$ErrorActionPreference = 'SilentlyContinue'
$root = (git rev-parse --show-toplevel 2>$null)
if (-not $root) { exit 0 }
Set-Location $root

$branch = git branch --show-current
$all = @(git status --short)
$deleted = @($all | Where-Object { $_ -match '^\s*D' })
$status = @($all | Where-Object { $_ -notmatch '^\s*D' } | Select-Object -First 15)
if ($deleted.Count) { $status += "($($deleted.Count) deleted files not listed)" }
$commits = @(git log --oneline -3)
$issue = if ($branch -match '(\d+)') { "#$($Matches[1])" } else { 'unknown' }

$text = "Branch: $branch (issue guess: $issue)`nRecent commits:`n  " + ($commits -join "`n  ")
$text += if ($status.Count) { "`nUncommitted (max 15):`n  " + ($status -join "`n  ") } else { "`nWorking tree clean." }

$handoff = Join-Path $root '.agent-state/handoff.md'
if (Test-Path $handoff) {
    $note = (Get-Content -LiteralPath $handoff -TotalCount 40) -join "`n"
    $text += "`n`nHandoff note from previous session (.agent-state/handoff.md):`n$note"
}

$lessons = Join-Path $root '.claude/lessons-learned.md'
if (Test-Path $lessons) {
    $titles = @(Get-Content -LiteralPath $lessons | Where-Object { $_ -match '^(## |### )' } | ForEach-Object { ($_ -replace '^### ', '  - ') -replace '^## ', '' })
    if ($titles.Count) {
        $text += "`n`nLessons learned (.claude/lessons-learned.md; grep it for details before implement/fix work, add entries via skill lessons-learned):`n" + ($titles -join "`n")
    }
}

@{ hookSpecificOutput = @{ hookEventName = 'SessionStart'; additionalContext = $text } } | ConvertTo-Json -Depth 5 -Compress
