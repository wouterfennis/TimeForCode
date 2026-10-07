# Stop hook: warns (never blocks) when C# files changed after the last successful dotnet build/test.
# quiet-run.ps1 writes .agent-state/last-dotnet-run on every successful run.
$ErrorActionPreference = 'SilentlyContinue'
$root = (git rev-parse --show-toplevel 2>$null)
if (-not $root) { exit 0 }
Set-Location $root

$changed = @(git status --short --untracked-files=all | Where-Object { $_ -notmatch '^\s*D' } |
    ForEach-Object { $_.Substring(3).Trim('"') } | Where-Object { $_ -match '\.cs$' -and (Test-Path -LiteralPath $_) })
if (-not $changed.Count) { exit 0 }

$stampFile = Join-Path $root '.agent-state/last-dotnet-run'
$stamp = if (Test-Path $stampFile) { (Get-Item $stampFile).LastWriteTime } else { [datetime]::MinValue }
$stale = @($changed | Where-Object { (Get-Item -LiteralPath $_).LastWriteTime -gt $stamp })
if ($stale.Count) {
    @{ systemMessage = "$($stale.Count) C# file(s) changed since the last successful dotnet build/test (e.g. $($stale[0])). Run a scoped build + test before finishing." } | ConvertTo-Json -Compress
}
exit 0
