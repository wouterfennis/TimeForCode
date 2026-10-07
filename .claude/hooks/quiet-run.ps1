# Runs a base64-encoded `dotnet build|test ...` command and prints only the useful lines.
# The full output is saved to a temp log; its path is printed when something failed. Exit code is preserved.
param([Parameter(Mandatory)][string]$Encoded)

$command = [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($Encoded))
if ($command -notmatch '^dotnet\s+(build|test)\b') { Write-Error 'quiet-run only wraps dotnet build/test'; exit 2 }
$kind = $Matches[1]

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

$keep | Select-Object -First $maxLines
if (@($keep).Count -gt $maxLines) { "... (+$(@($keep).Count - $maxLines) more lines)" }
if ($exit -ne 0) { "[exit code $exit] full output: $log" }
exit $exit
