#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Checks that a GitHub issue carries a human approval marker for a workflow phase.

.DESCRIPTION
    An approval is an issue comment whose first line is exactly `APPROVED: plan` or `APPROVED: feature`.
    Exit code 0 when the marker exists, 1 when it is missing (post `APPROVED: <phase>` on the issue).

.EXAMPLE
    .\scripts\check-approval.ps1 -Issue 111 -Phase plan
#>
param(
    [Parameter(Mandatory)] [int] $Issue,
    [Parameter(Mandatory)] [ValidateSet("plan", "feature")] [string] $Phase
)

$ErrorActionPreference = "Stop"

$jq = "[.comments[] | select(.body | test(`"^APPROVED: $Phase`"; `"m`")) | .author.login] | join(`",`")"
$authors = gh issue view $Issue --json comments --jq $jq

if ($authors) {
    Write-Output "APPROVED: $Phase found on #$Issue (by $authors)"
    exit 0
}

Write-Output "Missing marker: post `APPROVED: $Phase` as a comment on #$Issue."
exit 1
