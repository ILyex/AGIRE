#requires -Version 5.1
[CmdletBinding()]
param(
    [string]$InstallDirectory = $PSScriptRoot,
    [int]$Keep = 14
)

$ErrorActionPreference = 'Stop'
$dataDirectory = Join-Path $InstallDirectory 'data'
$database = Join-Path $dataDirectory 'hawdh.local.db'
$backupDirectory = Join-Path $dataDirectory 'backups'
$startScript = Join-Path $InstallDirectory 'Start-HawdhLocal.cmd'
New-Item -ItemType Directory -Force -Path $backupDirectory | Out-Null

@(Get-Process -Name 'Hawdh.Portal' -ErrorAction SilentlyContinue) | ForEach-Object { Stop-Process -Id $_.Id -Force }
Start-Sleep -Milliseconds 500
try {
    if (-not (Test-Path $database)) { throw "Database not found: $database" }
    $destination = Join-Path $backupDirectory ("hawdh-{0}.db" -f (Get-Date -Format 'yyyyMMdd-HHmmss'))
    Copy-Item $database $destination -Force
    if ((Get-Item $destination).Length -le 0) { throw 'The backup file is empty.' }
    Get-ChildItem $backupDirectory -Filter 'hawdh-*.db' | Sort-Object LastWriteTime -Descending | Select-Object -Skip $Keep | Remove-Item -Force
    Write-Host "Backup created: $destination"
}
finally {
    Start-Process -FilePath $startScript -WorkingDirectory $InstallDirectory -WindowStyle Hidden | Out-Null
}
