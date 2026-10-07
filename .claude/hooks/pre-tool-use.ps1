# PreToolUse hook (Claude Code). Reads the event JSON from stdin.
#  1. Denies reads/searches of paths that only burn tokens (node_modules, obj, .git internals, lockfiles, binaries).
#  2. Rewrites plain `dotnet build|test` terminal commands so only a trimmed result enters the context.
# Fails open by design: any error allows the tool call (a broken hook must not block work).
# permissions.deny in settings.json is the hard backstop for the Read tool.
$ErrorActionPreference = 'Stop'

function Send-Decision([string]$decision, [string]$reason, $updatedInput = $null) {
    $out = @{ hookSpecificOutput = @{ hookEventName = 'PreToolUse'; permissionDecision = $decision; permissionDecisionReason = $reason } }
    if ($null -ne $updatedInput) { $out.hookSpecificOutput.updatedInput = $updatedInput }
    $out | ConvertTo-Json -Depth 6 -Compress
    exit 0
}

try {
    $event = [Console]::In.ReadToEnd() | ConvertFrom-Json
    $toolName = [string]$event.tool_name
    $toolInput = $event.tool_input
    if ($null -eq $toolInput) { exit 0 }

    $command = $null
    foreach ($key in 'command', 'cmd') {
        if ($toolInput.PSObject.Properties[$key] -and $toolInput.$key -is [string]) { $command = $toolInput.$key; break }
    }

    $blockedDirs = '(^|[\\/])(node_modules|obj|\.git|\.vs|TestResults)([\\/]|$)'
    $blockedFiles = '(package-lock\.json|yarn\.lock|pnpm-lock\.yaml|packages\.lock\.json)$|\.(dll|exe|pdb|png|jpe?g|gif|ico|zip|nupkg|woff2?)$'

    if ($command) {
        # --- terminal tool ---
        if ($command -match '\.claude[\\/]hooks[\\/]quiet-run\.ps1') { exit 0 }

        # Escape hatch: a deliberate read of an excluded path may carry the marker `# allow-excluded`.
        # Quoted strings are ignored when matching, so `grep "node_modules" .gitignore` is not a read of node_modules.
        $unquoted = $command -replace '"[^"]*"', '""' -replace "'[^']*'", "''"
        $readVerbs = '(?i)(^|[\s;|&(])(cat|type|head|tail|grep|rg|findstr|select-string|get-content|gc|find|tree|get-childitem|gci|ls|dir)\b'
        if ($command -notmatch '#\s*allow-excluded' -and $unquoted -match $readVerbs -and $unquoted -match '(?i)(node_modules|[\\/]obj[\\/]|[\\/]\.git[\\/]|package-lock\.json|yarn\.lock)') {
            Send-Decision 'deny' 'Blocked by token-saving hook: node_modules, obj, .git internals and lockfiles are excluded. Use the Grep/Glob tools on src/, tst/ or docs/, or append "# allow-excluded" to the command if this read is intentional.'
        }

        # Single simple `dotnet build|test` (optionally with a trailing 2>&1) -> run via the trimming wrapper.
        $simple = ($command -replace '\s+2>&1\s*$', '').Trim()
        if ($simple -match '^dotnet\s+(build|test)\b' -and $simple -notmatch '[|;&<>`$]') {
            $encoded = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($simple))
            $projectDir = if ($env:CLAUDE_PROJECT_DIR) { $env:CLAUDE_PROJECT_DIR } elseif ($event.cwd) { [string]$event.cwd } else { [string](git rev-parse --show-toplevel) }
            $script = ($projectDir.TrimEnd('\', '/') + '/.claude/hooks/quiet-run.ps1') -replace '\\', '/'
            $wrapped = "pwsh -NoProfile -File `"$script`" -Encoded $encoded"
            $updated = @{}
            foreach ($p in $toolInput.PSObject.Properties) { $updated[$p.Name] = $p.Value }
            $updated['command'] = $wrapped
            Send-Decision 'allow' 'dotnet output trimmed by token-saving hook (full log path is printed on failure).' $updated
        }
        exit 0
    }

    # --- file read / search tools (edit tools are never blocked) ---
    if ($toolName -match '(?i)edit|write|create|replace|rename|delete|patch') { exit 0 }
    if ($toolName -notmatch '(?i)read|view|open|cat|search|grep|glob|find|list') { exit 0 }

    foreach ($prop in $toolInput.PSObject.Properties) {
        $isGlobPattern = ($toolName -eq 'Glob' -and $prop.Name -eq 'pattern')
        if ($prop.Name -notmatch '(?i)path|file|dir|folder|glob|include' -and -not $isGlobPattern) { continue }
        foreach ($value in @($prop.Value)) {
            if ($value -isnot [string]) { continue }
            if ($value.StartsWith('!')) { continue }   # exclusion glob, e.g. '!**/node_modules/**'
            if ($value -match $blockedDirs -or $value -match $blockedFiles) {
                Send-Decision 'deny' "Blocked by token-saving hook: '$value' is generated, binary or a lockfile. Read source files under src/, tst/ or docs/ instead."
            }
        }
    }
    exit 0
}
catch {
    exit 0
}
