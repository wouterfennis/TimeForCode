# Stop hook: warns (never blocks) when
#  1. C# files changed after the last successful dotnet build/test (quiet-run.ps1 writes .agent-state/last-dotnet-run), or
#  2. architecture-relevant files under src/ or deploy/ changed on this branch without a docs/architecture/arc42/ change.
$ErrorActionPreference = 'SilentlyContinue'
$root = (git rev-parse --show-toplevel 2>$null)
if (-not $root) { exit 0 }
Set-Location $root

$messages = @()
$status = @(git status --short --untracked-files=all | Where-Object { $_ -notmatch '^\s*D' } | ForEach-Object { $_.Substring(3).Trim('"') })

$changed = @($status | Where-Object { $_ -match '\.cs$' -and (Test-Path -LiteralPath $_) })
if ($changed.Count) {
    $stampFile = Join-Path $root '.agent-state/last-dotnet-run'
    $stamp = if (Test-Path $stampFile) { (Get-Item $stampFile).LastWriteTime } else { [datetime]::MinValue }
    $stale = @($changed | Where-Object { (Get-Item -LiteralPath $_).LastWriteTime -gt $stamp })
    if ($stale.Count) {
        $messages += "$($stale.Count) C# file(s) changed since the last successful dotnet build/test (e.g. $($stale[0])). Run a scoped build + test before finishing."
    }
}

# Arc42 sync: branch changes (committed + working tree) vs main.
$base = (git merge-base HEAD main 2>$null)
$branchFiles = @(if ($base) { git diff --name-only $base HEAD 2>$null }) + $status | Sort-Object -Unique
$architectural = @($branchFiles | Where-Object {
    $_ -match '^src/.*(\.csproj|Program\.cs|Controller\.cs)$' -or $_ -match '^deploy/' -or $_ -match '(^|/)(docker|podman)?-?compose[^/]*\.ya?ml$' -or $_ -match '(^|/)Dockerfile'
})
if ($architectural.Count -and -not @($branchFiles | Where-Object { $_ -match '^docs/architecture/arc42/' }).Count) {
    $messages += "Architecture-relevant files changed (e.g. $($architectural[0])) but docs/architecture/arc42/ was not updated. Update the arc42 docs if a service, API boundary or dependency changed."
}

if ($messages.Count) { @{ systemMessage = ($messages -join ' ') } | ConvertTo-Json -Compress }
exit 0