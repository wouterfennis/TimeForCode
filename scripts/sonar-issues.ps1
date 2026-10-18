<#
.SYNOPSIS
  Compact, read-only view of open SonarCloud findings for TimeForCode.
.DESCRIPTION
  Calls the SonarCloud web API and prints small projections so an agent does not read the full report.
  Auth: set SONAR_TOKEN (user token, "Execute analysis" is not needed, browse permission is enough).
  Public projects also work without a token. The token is only sent as a Bearer header, never printed.
.EXAMPLE
  pwsh scripts/sonar-issues.ps1 -Mode summary
  pwsh scripts/sonar-issues.ps1 -Mode list -Rule csharpsquid:S1128 -Limit 20
  pwsh scripts/sonar-issues.ps1 -Mode rule -Rule csharpsquid:S1128
  pwsh scripts/sonar-issues.ps1 -Mode hotspots
  pwsh scripts/sonar-issues.ps1 -Mode gate -PullRequest 130
#>
[CmdletBinding()]
param(
    [ValidateSet('summary', 'list', 'rule', 'hotspots', 'gate')]
    [string]$Mode = 'summary',
    [string]$Project = 'wouterfennis_TimeForCode',
    [string]$Organization = 'wouterfennis',
    [string]$Branch,
    [string]$PullRequest,
    [string]$Rule,
    [string]$Severities,
    [string]$Path,
    [int]$Limit = 50
)

$ErrorActionPreference = 'Stop'
$base = 'https://sonarcloud.io/api'
$headers = @{}
if ($env:SONAR_TOKEN) { $headers['Authorization'] = "Bearer $($env:SONAR_TOKEN)" }

function Invoke-Sonar([string]$Endpoint, [hashtable]$Query) {
    $qs = ($Query.GetEnumerator() | Where-Object { $_.Value } |
        ForEach-Object { '{0}={1}' -f $_.Key, [uri]::EscapeDataString([string]$_.Value) }) -join '&'
    try {
        Invoke-RestMethod -Uri "${base}/${Endpoint}?${qs}" -Headers $headers -Method Get
    }
    catch {
        $code = $_.Exception.Response.StatusCode.value__
        throw "SonarCloud $Endpoint failed (HTTP $code). 401/403: set SONAR_TOKEN; 404: check -Project/-Organization."
    }
}

$scope = @{ branch = $Branch; pullRequest = $PullRequest }

switch ($Mode) {
    'summary' {
        $r = Invoke-Sonar 'issues/search' ($scope + @{
                componentKeys = $Project; organization = $Organization; resolved = 'false'
                facets = 'rules,severities,types,directories'; ps = 1; severities = $Severities
            })
        "Open issues: $($r.total)"
        foreach ($f in 'severities', 'types', 'rules', 'directories') {
            "`n[$f]"
            ($r.facets | Where-Object property -eq $f).values | Select-Object -First 15 |
                ForEach-Object { '{0,5}  {1}' -f $_.count, $_.val }
        }
    }
    'list' {
        $r = Invoke-Sonar 'issues/search' ($scope + @{
                componentKeys = $Project; organization = $Organization; resolved = 'false'
                rules = $Rule; severities = $Severities; files = $Path; ps = $Limit; s = 'SEVERITY'; asc = 'false'
            })
        "Showing $($r.issues.Count) of $($r.total)"
        foreach ($i in $r.issues) {
            $file = $i.component -replace '^[^:]+:', ''
            '{0} | {1} | {2} | {3}:{4} | {5}' -f $i.key, $i.severity, $i.rule, $file, $i.line, $i.message
        }
    }
    'rule' {
        if (-not $Rule) { throw '-Rule is required for -Mode rule' }
        $r = (Invoke-Sonar 'rules/show' @{ key = $Rule; organization = $Organization }).rule
        "$($r.key) [$($r.severity)] $($r.name)"
        # Strip HTML, keep the description short.
        $text = ($r.htmlDesc ?? (($r.descriptionSections | ForEach-Object content) -join ' ')) -replace '<[^>]+>', ' ' -replace '\s+', ' '
        $text.Substring(0, [Math]::Min(1500, $text.Length))
    }
    'hotspots' {
        $r = Invoke-Sonar 'hotspots/search' ($scope + @{
                projectKey = $Project; organization = $Organization; status = 'TO_REVIEW'; ps = $Limit
            })
        "Hotspots to review: $($r.paging.total)"
        foreach ($h in $r.hotspots) {
            $file = $h.component -replace '^[^:]+:', ''
            '{0} | {1} | {2}:{3} | {4}' -f $h.key, $h.vulnerabilityProbability, $file, $h.line, $h.message
        }
    }
    'gate' {
        $r = Invoke-Sonar 'qualitygates/project_status' ($scope + @{ projectKey = $Project })
        "Quality gate: $($r.projectStatus.status)"
        $r.projectStatus.conditions | Where-Object status -ne 'OK' |
            ForEach-Object { '{0}: {1} (threshold {2}, actual {3})' -f $_.metricKey, $_.status, $_.errorThreshold, $_.actualValue }
    }
}
