param(
  [string] $SourceDir = "assets/test-media/loadout/elements",
  [string] $Bucket = "deadman",
  [string] $GameId = "c6c6a0da-0bd1-4f0b-bb2f-9a4c9c8b7f6a",
  [string] $Group = "elements",
  [string] $Endpoint = "http://deadmans-minio:9000",
  [string] $DockerNetwork = "dead-mans_default",
  [int] $Retries = 5,
  [int] $DelaySeconds = 2
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")

function Import-EnvFile {
  param([string] $Path)

  if (!(Test-Path -LiteralPath $Path)) {
    return
  }

  foreach ($line in Get-Content -LiteralPath $Path) {
    $trimmed = $line.Trim()
    if ([string]::IsNullOrWhiteSpace($trimmed) -or $trimmed.StartsWith("#")) {
      continue
    }

    $idx = $trimmed.IndexOf("=")
    if ($idx -le 0) { continue }

    $key = $trimmed.Substring(0, $idx).Trim()
    $value = $trimmed.Substring($idx + 1).Trim().Trim('"')

    if ($key) {
      if (-not (Test-Path env:$key)) {
        # set only if not already defined
        Set-Item -Path "env:$key" -Value $value
      }
    }
  }
}

Import-EnvFile (Join-Path $repoRoot ".env")

if (-not $env:MINIO_ROOT_USER -or -not $env:MINIO_ROOT_PASSWORD) {
  throw "Missing MinIO credentials. Ensure .env has MINIO_ROOT_USER and MINIO_ROOT_PASSWORD."
}

$sourceAbs = Resolve-Path (Join-Path $repoRoot $SourceDir)
if (!(Test-Path -LiteralPath $sourceAbs)) {
  throw "Source directory not found: $sourceAbs"
}

$accessKey = $env:MINIO_ROOT_USER
$secretKey = $env:MINIO_ROOT_PASSWORD

$alias = "deadman" # mc alias name
$dest = "$alias/$Bucket/games/$GameId/$Group/"

Write-Host "Uploading PNGs from '$sourceAbs' to '$dest'"

for ($attempt = 1; $attempt -le $Retries; $attempt++) {
  try {
    # Use mc in a disposable container; overwrite makes the operation idempotent.
    $cmd = @(
      "mc alias set `"$alias`" `"$Endpoint`" `"$accessKey`" `"$secretKey`" --api S3v4",
      "mc cp /data/*.png `"$dest`""
    ) -join "; "

    & docker run --rm `
      --network $DockerNetwork `
      --entrypoint sh `
      -v "${sourceAbs}:/data:ro" `
      minio/mc:latest `
      -lc $cmd

    if ($LASTEXITCODE -ne 0) {
      throw "mc upload failed with exit code $LASTEXITCODE"
    }

    Write-Host "Upload succeeded (attempt $attempt)."
    break
  }
  catch {
    if ($attempt -eq $Retries) {
      throw
    }

    Write-Host "Upload failed (attempt $attempt): $($_.Exception.Message). Retrying in $DelaySeconds s..."
    Start-Sleep -Seconds $DelaySeconds
  }
}

Write-Host "Done."

