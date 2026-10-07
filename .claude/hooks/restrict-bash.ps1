# PreToolUse hook used in subagent frontmatter to enforce a terminal allowlist.
# Usage: restrict-bash.ps1 -Allow '^gh issue (view|comment)\b','^dotnet (build|test)\b'
# A command is allowed when every statement (split on ; && || |) matches one pattern. Otherwise denied.
param([string[]]$Allow)

$ErrorActionPreference = 'Stop'
try {
    $event = [Console]::In.ReadToEnd() | ConvertFrom-Json
    $command = [string]$event.tool_input.command
    if (-not $command) { exit 0 }

    # Quoted/here-string bodies may contain operators; strip them before splitting.
    $flat = $command -replace '(?s)@"(.*?)"@', '""' -replace "(?s)@'(.*?)'@", "''" -replace '"[^"]*"', '""' -replace "'[^']*'", "''"
    $parts = $flat -split '\s*(?:;|&&|\|\||\|)\s*' | Where-Object { $_.Trim() }
    foreach ($part in $parts) {
        $p = $part.Trim() -replace '^\$\w+\s*=\s*', ''
        if (-not ($Allow | Where-Object { $p -match $_ })) {
            $reason = "Blocked: this agent may only run commands matching: $($Allow -join ' , '). Rejected: '$p'"
            @{ hookSpecificOutput = @{ hookEventName = 'PreToolUse'; permissionDecision = 'deny'; permissionDecisionReason = $reason } } | ConvertTo-Json -Depth 5 -Compress
            exit 0
        }
    }
}
catch {
    [Console]::Error.WriteLine("restrict-bash hook failed closed: $($_.Exception.Message). Fix .claude/hooks/restrict-bash.ps1.")
    exit 2
}
exit 0
