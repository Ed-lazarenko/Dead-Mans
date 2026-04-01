param(
  [switch]$Force
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

function Assert-LastExitCode {
  param(
    [Parameter(Mandatory = $true)][string]$Step
  )

  if ($LASTEXITCODE -ne 0) {
    throw "$Step failed with exit code $LASTEXITCODE."
  }
}

function Stop-RunningBackendProcess {
  $backendProcesses = Get-CimInstance Win32_Process -Filter "name = 'backend.exe'" -ErrorAction SilentlyContinue
  if ($null -eq $backendProcesses) {
    return
  }

  foreach ($process in $backendProcesses) {
    Stop-Process -Id $process.ProcessId -Force -ErrorAction SilentlyContinue
  }
}

if (-not $Force) {
  Write-Host "WARNING: This script deletes local PostgreSQL and MinIO Docker volumes." -ForegroundColor Yellow
  Write-Host "Type RESET to continue:" -ForegroundColor Yellow
  $confirmation = Read-Host
  if ($confirmation -cne "RESET") {
    Write-Host "Reset cancelled."
    exit 0
  }
}

$scriptDir = Split-Path -Parent $PSCommandPath
$backendRoot = Split-Path -Parent $scriptDir
$repoRoot = Split-Path -Parent $backendRoot
$setupScriptPath = Join-Path $scriptDir "setup-local.ps1"

Push-Location $repoRoot
try {
  Write-Host "Stopping running backend processes..."
  Stop-RunningBackendProcess

  Write-Host "Resetting Docker volumes..."
  docker compose down -v
  Assert-LastExitCode -Step "docker compose down -v"

  & $setupScriptPath -AfterReset
  Assert-LastExitCode -Step "setup-local.ps1"
}
finally {
  Pop-Location
}
