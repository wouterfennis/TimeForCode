# PreToolUse hook (VS Code Copilot + Claude Code). Reads the event JSON from stdin.
#  1. Denies reads/searches of paths that only burn tokens (node_modules, obj, .git internals, lockfiles, binaries).
#  2. Rewrites plain `dotnet build|test` terminal commands so only a trimmed result enters the context.
# Matchers are ignored by VS Code, so every filter lives here. Fails open: any error allows the tool call.
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
        if ($command -match '\.github[\\/]hooks[\\/]scripts[\\/]quiet-run\.ps1') { exit 0 }

        $readVerbs = '(?i)(^|[\s;|&(])(cat|type|head|tail|grep|rg|findstr|select-string|get-content|gc|find|tree|get-childitem|gci|ls|dir)\b'
        if ($command -match $readVerbs -and $command -match '(?i)(node_modules|[\\/]obj[\\/]|[\\/]\.git[\\/]|package-lock\.json|yarn\.lock)') {
            Send-Decision 'deny' 'Blocked by token-saving hook: node_modules, obj, .git internals and lockfiles are excluded. Search src/, tst/ or docs/ instead.'
        }

        # Single simple `dotnet build|test` (optionally with a trailing 2>&1) -> run via the trimming wrapper.
        $simple = ($command -replace '\s+2>&1\s*$', '').Trim()
        if ($simple -match '^dotnet\s+(build|test)\b' -and $simple -notmatch '[|;&<>`$]') {
            $encoded = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($simple))
            $wrapped = "pwsh -NoProfile -File .github/hooks/scripts/quiet-run.ps1 -Encoded $encoded"
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
        if ($prop.Name -notmatch '(?i)path|file|dir|folder|glob|include') { continue }
        foreach ($value in @($prop.Value)) {
            if ($value -isnot [string]) { continue }
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
