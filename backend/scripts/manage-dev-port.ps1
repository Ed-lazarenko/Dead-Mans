[CmdletBinding()]
param(
  [Parameter(Mandatory = $true)]
  [ValidateSet("Ensure", "Stop")]
  [string] $Action,

  [Parameter(Mandatory = $true)]
  [ValidateRange(1, 65535)]
  [int] $Port,

  [Parameter(Mandatory = $true)]
  [ValidateLength(1, 80)]
  [string] $Label
)

$ErrorActionPreference = "Stop"
$listeners = @(Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue)

if ($listeners.Count -eq 0) {
  if ($Action -eq "Stop") {
    Write-Host "  Port $Port ($Label) is free."
  }

  exit 0
}

$processIds = @($listeners | ForEach-Object { $_.OwningProcess } | Sort-Object -Unique)
if ($Action -eq "Ensure") {
  Write-Host ""
  Write-Host "  Port $Port ($Label) is busy."
  foreach ($processId in $processIds) {
    $process = Get-Process -Id $processId -ErrorAction SilentlyContinue
    $processName = if ($process) { $process.ProcessName } else { "?" }
    Write-Host "    PID $processId - $processName"
  }

  Write-Host ""
  $answer = Read-Host "  Stop these processes? [Y/n]"
  if ($answer -ne "" -and $answer -notmatch "^[yYdD]") {
    exit 1
  }
}

foreach ($processId in $processIds) {
  $process = Get-Process -Id $processId -ErrorAction SilentlyContinue
  $processName = if ($process) { $process.ProcessName } else { "?" }
  Write-Host "  Stopping PID $processId - $processName (port $Port)."
  Stop-Process -Id $processId -Force -ErrorAction SilentlyContinue
}

Start-Sleep -Milliseconds $(if ($Action -eq "Ensure") { 400 } else { 200 })
exit 0
