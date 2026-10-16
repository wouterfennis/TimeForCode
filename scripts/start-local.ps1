#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Starts the local TimeForCode stack using Podman Compose.

.DESCRIPTION
    On Windows, Podman requires a running Linux VM (Podman machine) before any
    container commands can be issued. This script detects whether the machine is
    running, starts (or initialises) it when necessary, and then delegates to
    `podman compose up --build`.

.EXAMPLE
    .\scripts\start-local.ps1
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Write-Info([string] $message) {
    Write-Output "`e[36m${message}`e[0m"
}

function Write-Success([string] $message) {
    Write-Output "`e[32m${message}`e[0m"
}

# ---------------------------------------------------------------------------
# Podman machine check (Windows only)
# ---------------------------------------------------------------------------

if ($IsWindows) {
    Write-Info "Checking Podman machine status..."

    $machineList = podman machine list --format json 2>&1 | ConvertFrom-Json -ErrorAction SilentlyContinue

    $runningMachine = $machineList | Where-Object { $_.Running -eq $true } | Select-Object -First 1

    if ($runningMachine) {
        Write-Success "Podman machine '$($runningMachine.Name)' is already running."
    } else {
        $existingMachine = $machineList | Select-Object -First 1

        if ($existingMachine) {
            Write-Info "Starting Podman machine '$($existingMachine.Name)'..."
            podman machine start $existingMachine.Name
        } else {
            Write-Info "No Podman machine found. Initialising a new one (this takes a moment)..."
            podman machine init
            podman machine start
        }

        Write-Success "Podman machine is running."
    }
}

# ---------------------------------------------------------------------------
# Start the stack
# ---------------------------------------------------------------------------

Write-Info "Starting the TimeForCode stack..."
podman compose up --build
