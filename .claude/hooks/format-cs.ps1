# PostToolUse hook (Edit|Write|MultiEdit): formats a just-edited .cs file with dotnet format.
# Silent on success; never blocks (always exits 0).
$ErrorActionPreference = 'SilentlyContinue'
try {
    $event = [Console]::In.ReadToEnd() | ConvertFrom-Json
    $path = [string]$event.tool_input.file_path
    if (-not $path -or $path -notmatch '\.cs$' -or -not (Test-Path -LiteralPath $path)) { exit 0 }

    $root = (git rev-parse --show-toplevel 2>$null)
    if (-not $root) { exit 0 }
    Set-Location $root

    $relative = [IO.Path]::GetRelativePath($root, (Resolve-Path -LiteralPath $path).Path) -replace '\\', '/'
    dotnet format whitespace . --folder --include $relative *> $null
}
catch { }
exit 0
