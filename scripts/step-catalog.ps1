<#
.SYNOPSIS
    Catalogues the Reqnroll steps of the specification projects.

.DESCRIPTION
    Reads the step attributes in the C# step classes and the steps in the .feature files of
    every tst/<Module>/TimeForCode.<Module>.Specifications project. Reports the summary counts,
    step definitions without usages, feature steps without a definition or with several, and
    optionally every step definition with its file and usage count.

.PARAMETER Module
    Module to catalogue (Authorization, Donation, Reporting) or All (default).

.PARAMETER Steps
    Also list every step definition with its definition file and usage count.

.PARAMETER Markdown
    Emit the report as Markdown (the format of docs/current/reqnroll-baseline.md).

.EXAMPLE
    pwsh scripts/step-catalog.ps1 -Module Donation -Steps
#>
[CmdletBinding()]
param(
    [ValidateSet('All', 'Authorization', 'Donation', 'Reporting')]
    [string]$Module = 'All',
    [switch]$Steps,
    [switch]$Markdown
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$attributePattern = '\[(Given|When|Then|StepDefinition)\(\s*(@?)"((?:[^"\\]|\\.|"")*)"((?:\s*\+\s*(?:\w+|"[^"]*"))*)'
$constantPattern = 'const string (\w+)\s*=\s*"((?:[^"\\]|\\.)*)"'
$featureKeywords = 'Given|When|Then|And|But|\*'

# Reqnroll reads a step text with {int}/{float}/{string}/{word} or an optional "(text)" group as a cucumber expression and anything else as a regex.
function ConvertTo-StepRegex([string]$expression) {
    $isCucumber = $expression -match '\{(int|float|string|word)}' -or ($expression -notmatch '^\^' -and $expression -match '\([A-Za-z ]+\)')
    if (-not $isCucumber) {
        return ($expression -replace '^\^?', '^' -replace '\$?$', '$')
    }
    $regex = [regex]::Escape($expression)
    $regex = [regex]::Replace($regex, '\\\(([^()]*?)\\\)', '(?:$1)?')
    $regex = $regex.Replace('\{int}', '(-?\d+)').Replace('\{float}', '(-?\d+(?:[.,]\d+)?)')
    $regex = $regex.Replace('\{string}', '("[^"]*"|''[^'']*'')').Replace('\{word}', '(\S+)')
    return "^$regex$"
}

function Get-HandWrittenFiles([string]$specRoot) {
    Get-ChildItem -Path $specRoot -Filter *.cs -Recurse |
        Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' -and $_.Name -notmatch '\.(feature|g)\.cs$' }
}

function Get-StepDefinitions([string]$specRoot) {
    foreach ($file in Get-HandWrittenFiles $specRoot) {
        $lineNumber = 0
        $lines = Get-Content -LiteralPath $file.FullName
        $constants = @{}
        foreach ($m in [regex]::Matches(($lines -join "`n"), $constantPattern)) { $constants[$m.Groups[1].Value] = $m.Groups[2].Value }
        foreach ($line in $lines) {
            $lineNumber++
            foreach ($match in [regex]::Matches($line, $attributePattern)) {
                $text = $match.Groups[3].Value
                $text = if ($match.Groups[2].Value) { $text.Replace('""', '"') } else { $text.Replace('\\', '\').Replace('\"', '"') }
                # Concatenated literals and string constants: "^text " + Constant + "$"
                foreach ($part in ($match.Groups[4].Value -split '\+' | Where-Object { $_.Trim() })) {
                    $part = $part.Trim()
                    $text += if ($part.StartsWith('"')) { $part.Trim('"') } else { $constants[$part] }
                }
                [pscustomobject]@{
                    Keyword = $match.Groups[1].Value
                    Text    = $text
                    File    = $file.FullName.Substring($specRoot.Length + 1).Replace('\', '/')
                    Line    = $lineNumber
                    Regex   = [regex](ConvertTo-StepRegex $text)
                    Uses    = 0
                }
            }
        }
    }
}

# Returns the feature steps (with their scenario, so outlines can be expanded) and the raw line count.
function Get-FeatureSteps([string]$specRoot) {
    $found = New-Object System.Collections.Generic.List[object]
    $featureLines = 0
    $scenarioId = 0
    foreach ($file in Get-ChildItem -Path "$specRoot/Features" -Filter *.feature -Recurse) {
        $lines = Get-Content -LiteralPath $file.FullName
        $featureLines += $lines.Count
        $relative = $file.FullName.Substring($specRoot.Length + 1).Replace('\', '/')
        $scenario = $null
        $type = $null
        $inBackground = $false
        $inExamples = $false
        $inDocString = $false
        $headers = $null
        for ($i = 0; $i -lt $lines.Count; $i++) {
            $line = $lines[$i].Trim()
            if ($line -eq '"""') { $inDocString = -not $inDocString; continue }
            if ($inDocString -or $line -eq '' -or $line.StartsWith('#') -or $line.StartsWith('@')) { continue }
            if ($line -match '^Background:') {
                $inBackground = $true; $inExamples = $false; $type = $null
                continue
            }
            if ($line -match '^(Scenario Outline|Scenario Template|Scenario|Example):') {
                $scenario = [pscustomobject]@{
                    Id       = ++$scenarioId
                    Outline  = [bool]($line -match '^Scenario (Outline|Template)')
                    Examples = New-Object System.Collections.Generic.List[object]
                }
                $inBackground = $false; $inExamples = $false; $type = $null; $headers = $null
                continue
            }
            if ($line -match '^Examples:') { $inExamples = $true; $headers = $null; continue }
            if ($line.StartsWith('|')) {
                if ($inExamples) {
                    $cells = @($line.Trim('|').Split('|') | ForEach-Object { $_.Trim() })
                    if (-not $headers) { $headers = $cells }
                    else {
                        $row = @{}
                        for ($c = 0; $c -lt $headers.Count; $c++) { $row[$headers[$c]] = $cells[$c] }
                        $scenario.Examples.Add($row)
                    }
                }
                continue
            }
            if ($line -match "^($featureKeywords)\s+(.+)$") {
                if ($Matches[1] -in 'Given', 'When', 'Then') { $type = $Matches[1] }
                $found.Add([pscustomobject]@{
                        Type       = $type
                        Text       = $Matches[2].Trim()
                        File       = $relative
                        Line       = $i + 1
                        Background = $inBackground
                        Scenario   = $scenario
                    })
            }
        }
    }
    [pscustomobject]@{ Steps = $found; FeatureLines = $featureLines }
}

function Measure-Specification([string]$name) {
    $specRoot = Join-Path $root "tst/$name/TimeForCode.$name.Specifications"
    $definitions = @(Get-StepDefinitions $specRoot)
    $feature = Get-FeatureSteps $specRoot
    $featureSteps = $feature.Steps

    # Scenarios: every plain scenario plus every Examples row of an outline.
    $scenarios = @($featureSteps | Where-Object { -not $_.Background } | ForEach-Object { $_.Scenario } | Sort-Object Id -Unique)
    $scenarioCount = 0
    foreach ($s in $scenarios) { $scenarioCount += [Math]::Max(1, $s.Examples.Count) }

    $unmatched = New-Object System.Collections.Generic.List[string]
    $ambiguous = New-Object System.Collections.Generic.List[string]
    foreach ($step in $featureSteps) {
        $variants = @($step.Text)
        if ($step.Scenario -and $step.Scenario.Outline -and $step.Scenario.Examples.Count -gt 0) {
            $variants = foreach ($example in $step.Scenario.Examples) {
                $text = $step.Text
                foreach ($key in $example.Keys) { $text = $text.Replace("<$key>", $example[$key]) }
                $text
            }
        }
        $hit = @{}
        foreach ($variant in $variants) {
            $candidates = @($definitions | Where-Object { ($_.Keyword -eq 'StepDefinition' -or $_.Keyword -eq $step.Type) -and $_.Regex.IsMatch($variant) })
            if ($candidates.Count -eq 0) { $unmatched.Add("$($step.File):$($step.Line) $($step.Type) $variant") }
            elseif ($candidates.Count -gt 1) { $ambiguous.Add("$($step.File):$($step.Line) $variant") }
            foreach ($candidate in $candidates) { $hit[$candidate] = $true }
        }
        foreach ($candidate in $hit.Keys) { $candidate.Uses++ }
    }

    $supportLines = 0
    foreach ($file in Get-HandWrittenFiles $specRoot) {
        $supportLines += @(Get-Content -LiteralPath $file.FullName | Where-Object { $_.Trim() -ne '' }).Count
    }

    [pscustomobject]@{
        Module            = $name
        Scenarios         = $scenarioCount
        StepLines         = $featureSteps.Count
        DistinctStepTexts = @($featureSteps | Select-Object -ExpandProperty Text -Unique).Count
        StepAttributes    = $definitions.Count
        SupportLines      = $supportLines
        FeatureLines      = $feature.FeatureLines
        Definitions       = $definitions
        Unused            = @($definitions | Where-Object { $_.Uses -eq 0 })
        Unmatched         = $unmatched
        Ambiguous         = $ambiguous
    }
}

function Write-Report($results) {
    $total = [pscustomobject]@{ Module = 'Total' }
    foreach ($column in 'Scenarios', 'StepLines', 'DistinctStepTexts', 'StepAttributes', 'SupportLines', 'FeatureLines') {
        $total | Add-Member -NotePropertyName $column -NotePropertyValue ([int]($results | Measure-Object $column -Sum).Sum)
    }
    $rows = @($results) + $total
    if ($Markdown) {
        '| Module | Scenarios | Step lines | Distinct step texts | Step attributes | Hand-written C# lines | Feature lines |'
        '| --- | ---: | ---: | ---: | ---: | ---: | ---: |'
        foreach ($r in $rows) { "| $($r.Module) | $($r.Scenarios) | $($r.StepLines) | $($r.DistinctStepTexts) | $($r.StepAttributes) | $($r.SupportLines) | $($r.FeatureLines) |" }
        ''
    }
    else {
        $rows | Format-Table Module, Scenarios, StepLines, DistinctStepTexts, StepAttributes, SupportLines, FeatureLines -AutoSize | Out-String
    }

    foreach ($r in $results) {
        $heading = "$($r.Module): step definitions without usages ($($r.Unused.Count))"
        if ($Markdown) { "### $heading"; '' } else { $heading }
        if ($r.Unused.Count -eq 0) { if ($Markdown) { 'None.'; '' } else { '  none' } }
        else {
            foreach ($u in $r.Unused) {
                if ($Markdown) { "- ``$($u.File):$($u.Line)`` $($u.Keyword): $($u.Text)" }
                else { "  $($u.File):$($u.Line)  [$($u.Keyword)] $($u.Text)" }
            }
            if ($Markdown) { '' }
        }
        if ($r.Unmatched.Count -gt 0) { "$($r.Module): feature steps without a definition ($($r.Unmatched.Count))"; $r.Unmatched | ForEach-Object { "  $_" } }
        if ($r.Ambiguous.Count -gt 0) { "$($r.Module): feature steps with several definitions ($($r.Ambiguous.Count))"; $r.Ambiguous | ForEach-Object { "  $_" } }
    }

    if ($Steps) {
        foreach ($r in $results) {
            if ($Markdown) { "### $($r.Module): step catalog"; ''; '| Uses | Step | Definition |'; '| ---: | --- | --- |' }
            else { "$($r.Module): step catalog (uses, step, definition)" }
            foreach ($d in $r.Definitions | Sort-Object @{ Expression = 'Uses'; Descending = $true }, Text) {
                if ($Markdown) { "| $($d.Uses) | $($d.Keyword): $($d.Text) | ``$($d.File):$($d.Line)`` |" }
                else { '{0,4}  [{1}] {2}  ({3}:{4})' -f $d.Uses, $d.Keyword, $d.Text, $d.File, $d.Line }
            }
            if ($Markdown) { '' }
        }
    }
}

$modules = if ($Module -eq 'All') { 'Authorization', 'Donation', 'Reporting' } else { @($Module) }
$results = foreach ($name in $modules) { Measure-Specification $name }
Write-Report $results
