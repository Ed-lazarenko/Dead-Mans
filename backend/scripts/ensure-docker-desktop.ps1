param(
  [int]$TimeoutSeconds = 120
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

function Test-DockerEngine {
  docker info --format "{{.ServerVersion}}" *> $null
  return $LASTEXITCODE -eq 0
}

if (Test-DockerEngine) {
  Write-Host "Docker Engine is already running."
  exit 0
}

$dockerDesktop = "C:\Program Files\Docker\Docker\Docker Desktop.exe"
if (-not (Test-Path -LiteralPath $dockerDesktop)) {
  throw "Docker Desktop is not installed at '$dockerDesktop'."
}

$localAppData = [Environment]::GetFolderPath("LocalApplicationData")
$dockerLocal = Join-Path $localAppData "Docker"
$quarantineRoot = Join-Path $dockerLocal "runtime-quarantine"

Get-Process -Name "Docker Desktop", "com.docker.backend" -ErrorAction SilentlyContinue |
  Stop-Process -Force -ErrorAction SilentlyContinue

New-Item -ItemType Directory -Path $quarantineRoot -Force | Out-Null
$stamp = "{0}-{1}" -f (Get-Date -Format "yyyyMMdd-HHmmssfff"), $PID

function Move-TransientRuntimeDirectory {
  param(
    [Parameter(Mandatory = $true)][string]$Source,
    [Parameter(Mandatory = $true)][string]$Label
  )

  if (-not (Test-Path -LiteralPath $Source)) {
    return
  }

  $destination = Join-Path $quarantineRoot "$Label-$stamp"
  Move-Item -LiteralPath $Source -Destination $destination
  Write-Host "Quarantined stale Docker runtime directory: $Label"
}

Move-TransientRuntimeDirectory -Source (Join-Path $dockerLocal "run") -Label "run"
Move-TransientRuntimeDirectory `
  -Source (Join-Path $localAppData "docker-secrets-engine") `
  -Label "secrets-engine"

Write-Host "Starting Docker Desktop..."
Start-Process -FilePath $dockerDesktop -WindowStyle Hidden

$deadline = (Get-Date).AddSeconds($TimeoutSeconds)
do {
  Start-Sleep -Seconds 2
  if (Test-DockerEngine) {
    Write-Host "Docker Engine is ready."
    exit 0
  }
} while ((Get-Date) -lt $deadline)

throw "Docker Engine did not become ready within $TimeoutSeconds seconds."
