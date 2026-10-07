# PreToolUse hook (Bash|PowerShell). Guards against risky terminal commands:
#  1. secrets: any command touching .env*, key/cert files or secrets.json is denied.
#  2. destructive git/file ops: force push and `reset --hard` on main are denied; recursive deletes ask.
#  3. build/test gate: `git push` / `gh pr create` are denied while C# files are newer than the last successful dotnet run.
#  4. podman only: `docker compose` / `docker-compose` is denied.
# Fails closed: any internal error blocks the tool call (exit 2).
$ErrorActionPreference = 'Stop'

function Send-Decision([string]$decision, [string]$reason) {
    @{ hookSpecificOutput = @{ hookEventName = 'PreToolUse'; permissionDecision = $decision; permissionDecisionReason = $reason } } | ConvertTo-Json -Depth 5 -Compress
    exit 0
}

try {
    $event = [Console]::In.ReadToEnd() | ConvertFrom-Json
    $command = [string]$event.tool_input.command
    if (-not $command) { exit 0 }
    if ($command -match '\.claude[\/]hooks[\\/]quiet-run\.ps1') { exit 0 }

    # Quoted strings (commit messages, echo text) are ignored for the git/docker/delete checks.
    $unquoted = $command -replace '"[^"]*"', '""' -replace "'[^']*'", "''"
    $verb = '(^|[\s;|&(])'

    # --- 1. secrets (raw command: quoting a path must not bypass the guard) ---
    $secretPath = '(?i)(^|[\s"''=/\\])(\.env(\.(?!example|sample|template)[\w.-]+)?|secrets\.json|[\w.-]+\.(pfx|pem|key)|appsettings\.[\w.-]*local\.json)(?=$|[\s"''|;&)])'
    if ($command -match $secretPath) {
        Send-Decision 'deny' 'Blocked by secret guard: the command touches an env/secret/key file (.env*, secrets.json, *.pem, *.pfx, *.key). Never read, print or commit these; ask the user to run it with the ! prefix if it is really needed.'
    }
    if ($unquoted -match "${verb}git\s+commit\b") {
        $staged = @(git diff --cached --name-only 2>$null)
        $hit = $staged | Where-Object { $_ -match '(?i)(^|/)(\.env(\.(?!example|sample|template)[^/]+)?|secrets\.json|[^/]+\.(pfx|pem|key))$' } | Select-Object -First 1
        if ($hit) { Send-Decision 'deny' "Blocked by secret guard: '$hit' is staged. Unstage it (git restore --staged) before committing." }
    }

    # --- 4. podman only ---
    if ($unquoted -match "(?i)${verb}docker(-|\s+)compose\b") {
        Send-Decision 'deny' 'This project uses Podman, not Docker. Use `podman compose ...` (see scripts/start-local.ps1).'
    }

    # --- 2. destructive operations ---
    if ($unquoted -match "(?i)${verb}git\s+push\b[^;|&]*\s(--force\b|-f\b|--force-with-lease\b|\+\S)") {
        Send-Decision 'deny' 'Blocked: force push is not allowed. Push normally or ask the user to run it with the ! prefix.'
    }
    if ($unquoted -match "(?i)${verb}git\s+reset\b[^;|&]*--hard") {
        $branch = (git rev-parse --abbrev-ref HEAD 2>$null)
        if ($branch -in 'main', 'master') { Send-Decision 'deny' "Blocked: git reset --hard on '$branch' is not allowed." }
    }
    $recursiveDelete = "(?i)${verb}(rm\s+(-\w*[rR]\w*|--recursive)\b|remove-item\b[^;|&]*-recurse|rd\s+/s|rmdir\s+/s|del\s+[^;|&]*/s)"
    if ($unquoted -match $recursiveDelete -and $command -notmatch '#\s*confirmed') {
        Send-Decision 'ask' 'Recursive delete: confirm the target before it runs.'
    }

    # --- 3. build/test gate before push / PR ---
    if ($unquoted -match "(?i)${verb}(git\s+push|gh\s+pr\s+create)\b") {
        $root = (git rev-parse --show-toplevel 2>$null)
        if ($root) {
            Set-Location $root
            $changed = @(git status --short --untracked-files=all | Where-Object { $_ -notmatch '^\s*D' } |
                ForEach-Object { $_.Substring(3).Trim('"') } | Where-Object { $_ -match '\.cs$' -and (Test-Path -LiteralPath $_) })
            if ($changed.Count) {
                $stampFile = Join-Path $root '.agent-state/last-dotnet-run'
                $stamp = if (Test-Path $stampFile) { (Get-Item $stampFile).LastWriteTime } else { [datetime]::MinValue }
                $stale = @($changed | Where-Object { (Get-Item -LiteralPath $_).LastWriteTime -gt $stamp })
                if ($stale.Count) {
                    Send-Decision 'deny' "Build/test gate: $($stale.Count) uncommitted C# file(s) changed since the last successful dotnet build/test (e.g. $($stale[0])). Run dotnet build and dotnet test first."
                }
            }
        }
    }
    exit 0
}
catch {
    [Console]::Error.WriteLine("guard-bash hook failed closed: $($_.Exception.Message). Fix .claude/hooks/guard-bash.ps1.")
    exit 2
}
