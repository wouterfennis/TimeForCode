# Runs `dotnet format whitespace` on the solution and keeps the build/test stamp valid.
# If no uncommitted C# file was newer than .agent-state/last-dotnet-run before formatting, the stamp is
# refreshed afterwards: whitespace-only changes must not force another full build and test (guard-bash.ps1 gate).
$root = (git rev-parse --show-toplevel 2>$null)
if (-not $root) { Write-Error 'Not in a git repository.'; exit 1 }
Set-Location $root

function Get-StaleCount {
    $stampFile = Join-Path $root '.agent-state/last-dotnet-run'
    if (-not (Test-Path $stampFile)) { return 1 }
    $stamp = (Get-Item $stampFile).LastWriteTime
    @(git status --short --untracked-files=all | Where-Object { $_ -notmatch '^\s*D' } |
        ForEach-Object { $_.Substring(3).Trim('"') } |
        Where-Object { $_ -match '\.cs$' -and (Test-Path -LiteralPath $_) -and (Get-Item -LiteralPath $_).LastWriteTime -gt $stamp }).Count
}

$wasClean = (Get-StaleCount) -eq 0
$dirtyBefore = @(git diff --name-only)
dotnet format whitespace ./TimeForCode.sln
$code = $LASTEXITCODE

# The formatter touches files outside the branch's scope; restore those so they do not pollute the PR.
$inScope = @(git diff --name-only origin/main...HEAD) + $dirtyBefore
$churn = @(git diff --name-only | Where-Object { $_ -notin $inScope })
if ($churn.Count) {
    git checkout -- $churn
    Write-Output "Reverted formatter churn in $($churn.Count) file(s) outside the branch: $($churn -join ', ')"
}
if ($wasClean -and $code -eq 0) { (Get-Item (Join-Path $root '.agent-state/last-dotnet-run')).LastWriteTime = Get-Date }
exit $code
