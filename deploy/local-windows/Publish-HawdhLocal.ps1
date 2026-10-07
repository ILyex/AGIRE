#requires -Version 5.1
[CmdletBinding()]
param(
    [string]$OutputDirectory,
    [string]$DotnetPath = 'dotnet'
)

$ErrorActionPreference = 'Stop'
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..\..')
if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $repoRoot 'artifacts\hawdh-local-win-x64'
}
$project = Join-Path $repoRoot 'src\web\Hawdh.Portal\Hawdh.Portal.csproj'
$output = [System.IO.Path]::GetFullPath($OutputDirectory)
if (-not (Test-Path $project)) { throw "Project not found: $project" }
$payload = Join-Path $output 'payload'
$preservedData = Join-Path ([System.IO.Path]::GetTempPath()) ("hawdh-publish-data-" + [guid]::NewGuid().ToString('N'))
$existingData = Join-Path $payload 'data'
if (Test-Path $existingData) {
    # Keep the customer's local database, keys and setup marker when updating the package.
    Copy-Item $existingData $preservedData -Recurse -Force
}
if (Test-Path $output) { Get-ChildItem $output -Force | Remove-Item -Recurse -Force }
New-Item -ItemType Directory -Force -Path $payload | Out-Null

& $DotnetPath publish $project -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -o $payload
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed with exit code $LASTEXITCODE" }

$launcherProject = Join-Path $repoRoot 'src\tools\Hawdh.Launcher\Hawdh.Launcher.csproj'
& $DotnetPath publish $launcherProject -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o $output
if ($LASTEXITCODE -ne 0) { throw "launcher publish failed with exit code $LASTEXITCODE" }
if (Test-Path $preservedData) {
    New-Item -ItemType Directory -Force -Path (Join-Path $payload 'data') | Out-Null
    Copy-Item (Join-Path $preservedData '*') (Join-Path $payload 'data') -Recurse -Force
    Remove-Item $preservedData -Recurse -Force -ErrorAction SilentlyContinue
}
Rename-Item (Join-Path $output 'Hawdh.Launcher.exe') (Join-Path $output 'AGIRE.exe') -Force
Copy-Item (Join-Path $output 'AGIRE.exe') (Join-Path $payload 'AGIRE.exe') -Force
Copy-Item (Join-Path $output 'AGIRE.exe') (Join-Path $payload 'REST.exe') -Force
Copy-Item (Join-Path $output 'AGIRE.exe') (Join-Path $output 'AGIRE-Setup.exe') -Force
Copy-Item (Join-Path $repoRoot 'src\tools\Hawdh.Launcher\agire.ico') (Join-Path $payload 'agire.ico') -Force
Copy-Item (Join-Path $repoRoot 'src\tools\Hawdh.Launcher\Assets\agire-installer.png') (Join-Path $payload 'agire-installer.png') -Force
Set-Content -LiteralPath (Join-Path $output 'version.txt') -Value (git -C $repoRoot rev-parse --short HEAD) -Encoding ascii
Remove-Item (Join-Path $output 'AGIRE.exe') -Force -ErrorAction SilentlyContinue
Remove-Item (Join-Path $output 'Hawdh.Launcher.pdb') -Force -ErrorAction SilentlyContinue
Copy-Item (Join-Path $PSScriptRoot 'README.ar.md') (Join-Path $output 'README.txt') -Force
@('@echo off', 'setlocal', 'cd /d "%~dp0"', 'start "AGIRE" "%~dp0payload\AGIRE.exe"', 'timeout /t 5 /nobreak >nul', 'del /f /q "%PUBLIC%\Desktop\REST.lnk" 2>nul', 'del /f /q "%USERPROFILE%\Desktop\REST.lnk" 2>nul', 'del /f /q "%PUBLIC%\Desktop\تحديث AGIRE.lnk" 2>nul', 'del /f /q "%PUBLIC%\Desktop\فتح AGIRE.lnk" 2>nul', 'endlocal') | Set-Content -LiteralPath (Join-Path $output 'Install-AGIRE.cmd') -Encoding ascii
Copy-Item (Join-Path $PSScriptRoot 'Backup-HawdhLocal.ps1') (Join-Path $payload 'Backup-HawdhLocal.ps1') -Force
Copy-Item (Join-Path $PSScriptRoot 'Start-HawdhLocal.cmd') (Join-Path $payload 'Start-HawdhLocal.cmd') -Force
Copy-Item (Join-Path $PSScriptRoot 'Update-AGIRE.ps1') (Join-Path $output 'Update-AGIRE.ps1') -Force
Copy-Item (Join-Path $PSScriptRoot 'Update-AGIRE.ps1') (Join-Path $payload 'Update-AGIRE.ps1') -Force
Write-Host "Local Windows package created: $output"
