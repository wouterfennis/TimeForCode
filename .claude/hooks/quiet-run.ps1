# Runs a base64-encoded `dotnet build|test ...` command and prints only the useful lines.
# The full output is saved to a temp log; its path is printed when something failed. Exit code is preserved.
param([Parameter(Mandatory)][string]$Encoded)

$command = [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($Encoded))
if ($command -notmatch '^dotnet\s+(build|test)\b') { Write-Error 'quiet-run only wraps dotnet build/test'; exit 2 }
$kind = $Matches[1]

# Solution-wide runs are skipped when the same kind already passed and no source file changed since.
$isSolution = $command -match 'TimeForCode\.sln'
$root = (git rev-parse --show-toplevel 2>$null)
$fullStamp = if ($root) { Join-Path $root ".agent-state/last-full-$kind" } else { $null }
if ($isSolution -and $fullStamp -and (Test-Path -LiteralPath $fullStamp)) {
    $stampTime = (Get-Item -LiteralPath $fullStamp).LastWriteTime
    $files = git -C $root ls-files -co --exclude-standard -- src tst Directory.Build.props Directory.Packages.props TimeForCode.sln 2>$null |
        Where-Object { $_ -match '\.(cs|csproj|props|targets|feature|json|txt|sln)$' }
    $newer = $files | Where-Object { $p = Join-Path $root $_; (Test-Path -LiteralPath $p) -and ([IO.File]::GetLastWriteTime($p) -gt $stampTime) } | Select-Object -First 1
    if (-not $newer) {
        "Skipped: solution $kind already passed at $($stampTime.ToString('s')) and no source file changed since. Delete .agent-state/last-full-$kind to force a rerun."
        exit 0
    }
}
$startTime = Get-Date

$log = Join-Path ([IO.Path]::GetTempPath()) ("dotnet-$kind-" + (Get-Date -Format 'yyyyMMdd-HHmmss') + '.log')
$env:DOTNET_NOLOGO = '1'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:MSBUILDTERMINALLOGGER = 'off'

$global:LASTEXITCODE = 0
Invoke-Expression "$command 2>&1" | Out-File -FilePath $log -Encoding utf8
$exit = $LASTEXITCODE
$lines = Get-Content -LiteralPath $log

$maxLines = 60
if ($kind -eq 'build') {
    $keep = $lines | Where-Object { $_ -match '(?i): (error|warning) [A-Za-z]+\d+|Build succeeded|Build FAILED|^\s*\d+ (Warning|Error)\(s\)|Time Elapsed' } | Select-Object -Unique
}
else {
    $keep = $lines | Where-Object { $_ -match '^\s*(Passed!|Failed!|Skipped!)|^\s*(Total|Passed|Failed|Skipped)( tests)?:|^\s+Failed |Error Message|Assert|Expected|Actual|Exception|\.cs:line|error [A-Za-z]+\d+|Build FAILED|Test Run (Successful|Failed)' } | Select-Object -Unique
}
if (-not $keep) { $keep = $lines | Select-Object -Last 20 }

# Stamp a successful run so the Stop hook knows C# changes were verified.
if ($exit -eq 0) {
    $root = (git rev-parse --show-toplevel 2>$null)
    if ($root) {
        $dir = Join-Path $root '.agent-state'
        New-Item -ItemType Directory -Force -Path $dir | Out-Null
        Set-Content -LiteralPath (Join-Path $dir 'last-dotnet-run') -Value (Get-Date -Format 's') -Encoding utf8
        if ($isSolution) {
            $fullPath = Join-Path $dir "last-full-$kind"
            Set-Content -LiteralPath $fullPath -Value (Get-Date -Format 's') -Encoding utf8
            (Get-Item -LiteralPath $fullPath).LastWriteTime = $startTime
        }
    }
}

$keep | Select-Object -First $maxLines
if (@($keep).Count -gt $maxLines) { "... (+$(@($keep).Count - $maxLines) more lines)" }
if ($exit -ne 0) { "[exit code $exit] full output: $log" }
exit $exit
