param(
  [string]$Endpoint = "http://localhost:9000",
  [string]$Bucket = "deadman",
  [string]$GameId = "c6c6a0da-0bd1-4f0b-bb2f-9a4c9c8b7f6a",
  [string]$Group = "elements",
  [string]$SourceDir = ""
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

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

function Assert-LastExitCode {
  param(
    [Parameter(Mandatory = $true)][string]$Step
  )

  if ($LASTEXITCODE -ne 0) {
    throw "$Step failed with exit code $LASTEXITCODE."
  }
}

$scriptDir = Split-Path -Parent $PSCommandPath
$backendRoot = Split-Path -Parent $scriptDir
$repoRoot = Split-Path -Parent $backendRoot
$envFile = Join-Path $repoRoot ".env"

if (Test-Path $envFile) {
  Import-DotEnvFile -Path $envFile
}

$projectPath = Join-Path $backendRoot "tools\SeedTestGameBoardMedia\SeedTestGameBoardMedia.csproj"
$resolvedSourceDir = if ([string]::IsNullOrWhiteSpace($SourceDir)) {
  Join-Path $backendRoot "assets\test-game-board\elements"
} else {
  $SourceDir
}

Push-Location $repoRoot
try {
  dotnet run --project $projectPath -- `
    --endpoint=$Endpoint `
    --bucket=$Bucket `
    --gameId=$GameId `
    --group=$Group `
    --sourceDir=$resolvedSourceDir
  Assert-LastExitCode -Step "upload-test-game-board-media"
}
finally {
  Pop-Location
}
