param(
  [switch]$AfterReset
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

function Wait-ForContainerHealth {
  param(
    [Parameter(Mandatory = $true)][string]$ContainerName,
    [Parameter(Mandatory = $true)][string]$ExpectedHealth,
    [int]$TimeoutSeconds = 120
  )

  $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
  do {
    $status = docker inspect -f "{{.State.Health.Status}}" $ContainerName 2>$null
    if ($status -eq $ExpectedHealth) {
      return
    }

    Start-Sleep -Seconds 2
  } while ((Get-Date) -lt $deadline)

  throw "Container '$ContainerName' did not reach health state '$ExpectedHealth' within $TimeoutSeconds seconds."
}

function Wait-ForContainerRunning {
  param(
    [Parameter(Mandatory = $true)][string]$ContainerName,
    [int]$TimeoutSeconds = 120
  )

  $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
  do {
    $status = docker inspect -f "{{.State.Running}}" $ContainerName 2>$null
    if ($status -eq "true") {
      return
    }

    Start-Sleep -Seconds 2
  } while ((Get-Date) -lt $deadline)

  throw "Container '$ContainerName' did not start within $TimeoutSeconds seconds."
}

function Assert-LastExitCode {
  param(
    [Parameter(Mandatory = $true)][string]$Step
  )

  if ($LASTEXITCODE -ne 0) {
    throw "$Step failed with exit code $LASTEXITCODE."
  }
}

function Import-DotEnvFile {
  param(
    [Parameter(Mandatory = $true)][string]$Path
  )

  Get-Content $Path | ForEach-Object {
    $line = $_.Trim()
    if ([string]::IsNullOrWhiteSpace($line) -or $line.StartsWith("#")) {
      return
    }

    $parts = $line.Split("=", 2)
    if ($parts.Length -ne 2) {
      return
    }

    [System.Environment]::SetEnvironmentVariable($parts[0], $parts[1], "Process")
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

$scriptDir = Split-Path -Parent $PSCommandPath
$backendRoot = Split-Path -Parent $scriptDir
$repoRoot = Split-Path -Parent $backendRoot
$envFile = Join-Path $repoRoot ".env"
$envExampleFile = Join-Path $repoRoot ".env.example"
$backendProjectPath = Join-Path $backendRoot "backend.csproj"
$uploadScriptPath = Join-Path $scriptDir "upload-test-game-board-media.ps1"

if (-not (Test-Path $envFile)) {
  Copy-Item $envExampleFile $envFile
  Write-Host "Created local .env from .env.example"
}

Import-DotEnvFile -Path $envFile

Push-Location $repoRoot
try {
  Write-Host "Stopping running backend processes..."
  Stop-RunningBackendProcess

  Write-Host "Starting postgres and minio..."
  docker compose up -d
  Assert-LastExitCode -Step "docker compose up -d"

  Wait-ForContainerHealth -ContainerName "deadmans-postgres" -ExpectedHealth "healthy" -TimeoutSeconds 120
  Wait-ForContainerRunning -ContainerName "deadmans-minio" -TimeoutSeconds 120

  Write-Host "Applying EF Core migrations..."
  dotnet ef database update --project $backendProjectPath --startup-project $backendProjectPath
  Assert-LastExitCode -Step "dotnet ef database update"

  Write-Host "Uploading test game-board media to MinIO..."
  & $uploadScriptPath
  Assert-LastExitCode -Step "upload-test-game-board-media.ps1"

  Write-Host ""
  Write-Host "Backend local setup completed."
  if ($AfterReset) {
    Write-Host "Database and storage volumes were recreated by reset-local.ps1."
  } else {
    Write-Host "Existing database and storage volumes were preserved."
  }
  Write-Host "Backend:  http://localhost:5285"
  Write-Host "MinIO:    http://localhost:9000"
  Write-Host "Console:  http://localhost:9001"
}
finally {
  Pop-Location
}
