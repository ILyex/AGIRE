#requires -Version 5.1
[CmdletBinding()]
param(
    [string]$InstallDirectory = 'C:\ProgramData\AGIRE\Hawdh',
    [string]$Repository = 'ILyex/AGIRE'
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Windows.Forms
[System.Windows.Forms.Application]::EnableVisualStyles()

function Show-UpdateMessage([string]$Message, [string]$Title = 'AGIRE') {
    [System.Windows.Forms.MessageBox]::Show($Message, $Title, [System.Windows.Forms.MessageBoxButtons]::OK, [System.Windows.Forms.MessageBoxIcon]::Information) | Out-Null
}

try {
    if (-not (Test-Path (Join-Path $InstallDirectory 'Hawdh.Portal.exe'))) { throw 'لم يتم العثور على تثبيت AGIRE في المسار المحدد.' }
    $latest = Invoke-RestMethod -Uri "https://api.github.com/repos/$Repository/releases/latest" -Headers @{ 'User-Agent' = 'AGIRE-Updater'; Accept = 'application/vnd.github+json' } -TimeoutSec 20
    $archive = $latest.assets | Where-Object name -eq 'AGIRE-local-win-x64.zip' | Select-Object -First 1
    $checksum = $latest.assets | Where-Object name -eq 'AGIRE-local-win-x64.zip.sha256' | Select-Object -First 1
    if (-not $archive -or -not $checksum) { throw 'لم تُرفق حزمة Windows أو بصمتها مع آخر إصدار.' }

    $currentVersion = 'غير معروف'
    $versionFile = Join-Path $InstallDirectory 'version.txt'
    if (Test-Path $versionFile) { $currentVersion = (Get-Content $versionFile -Raw).Trim() }
    if ($currentVersion -eq $latest.tag_name) {
        Show-UpdateMessage "لديك آخر إصدار بالفعل: $currentVersion"
        exit 0
    }

    $choice = [System.Windows.Forms.MessageBox]::Show("يتوفر إصدار جديد من AGIRE: $($latest.tag_name)`nالإصدار الحالي: $currentVersion`n`nسيتم الاحتفاظ بقاعدة البيانات ومفاتيح الحماية. هل تريد تنزيل التحديث وتثبيته؟", 'تحديث AGIRE', [System.Windows.Forms.MessageBoxButtons]::YesNo, [System.Windows.Forms.MessageBoxIcon]::Question)
    if ($choice -ne [System.Windows.Forms.DialogResult]::Yes) { exit 0 }

    $admin = [Security.Principal.WindowsPrincipal]::new([Security.Principal.WindowsIdentity]::GetCurrent())
    if (-not $admin.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
        $arguments = "-NoProfile -ExecutionPolicy Bypass -File `"$PSCommandPath`" -InstallDirectory `"$InstallDirectory`" -Repository `"$Repository`""
        Start-Process -FilePath (Join-Path $env:SystemRoot 'System32\WindowsPowerShell\v1.0\powershell.exe') -Verb RunAs -ArgumentList $arguments
        exit 0
    }

    $work = Join-Path ([IO.Path]::GetTempPath()) ('AGIRE-update-' + [guid]::NewGuid().ToString('N'))
    $null = New-Item -ItemType Directory -Path $work
    $zip = Join-Path $work 'release.zip'
    $sumPath = Join-Path $work 'release.sha256'
    Invoke-WebRequest -Uri $archive.browser_download_url -OutFile $zip -Headers @{ 'User-Agent' = 'AGIRE-Updater' } -TimeoutSec 300
    Invoke-WebRequest -Uri $checksum.browser_download_url -OutFile $sumPath -Headers @{ 'User-Agent' = 'AGIRE-Updater' } -TimeoutSec 30
    $expected = ((Get-Content $sumPath -Raw).Trim() -split '\s+')[0].ToLowerInvariant()
    $actual = (Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($actual -ne $expected) { throw 'فشل التحقق من سلامة حزمة التحديث؛ لم يتم تغيير ملفات المنصة.' }

    $expanded = Join-Path $work 'expanded'
    Expand-Archive -LiteralPath $zip -DestinationPath $expanded
    $payload = Join-Path $expanded 'payload'
    if (-not (Test-Path (Join-Path $payload 'Hawdh.Portal.exe'))) { throw 'حزمة التحديث غير مكتملة؛ لم يتم تغيير ملفات المنصة.' }

    $appBackup = Join-Path $work 'previous-app'
    $null = New-Item -ItemType Directory -Path $appBackup
    Get-ChildItem -LiteralPath $InstallDirectory -File | Where-Object Name -ne 'Update-AGIRE.ps1' | Copy-Item -Destination $appBackup -Force
    $db = Join-Path $InstallDirectory 'data\hawdh.local.db'
    if (Test-Path $db) {
        $backups = Join-Path $InstallDirectory 'data\backups'
        $null = New-Item -ItemType Directory -Force -Path $backups
        Copy-Item $db (Join-Path $backups ("before-update-{0}.db" -f (Get-Date -Format 'yyyyMMdd-HHmmss'))) -Force
    }

    $startFile = Join-Path $InstallDirectory 'Start-HawdhLocal.cmd'
    if (Test-Path $startFile) { Get-CimInstance Win32_Process -Filter "Name='Hawdh.Portal.exe'" | ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue } }
    Start-Sleep -Milliseconds 700
    try {
        Get-ChildItem -LiteralPath $payload -File | Copy-Item -Destination $InstallDirectory -Force
        foreach ($name in @('README.txt')) {
            $source = Join-Path $expanded $name
            if (Test-Path $source) { Copy-Item $source (Join-Path $InstallDirectory $name) -Force }
        }
        Set-Content -LiteralPath (Join-Path $InstallDirectory 'version.txt') -Value $latest.tag_name -Encoding ascii
        if (Test-Path $startFile) { Start-Process -FilePath $startFile -WorkingDirectory $InstallDirectory -WindowStyle Hidden }
        Show-UpdateMessage "تم تحديث AGIRE بنجاح إلى الإصدار $($latest.tag_name). بقيت قاعدة البيانات كما هي."
    }
    catch {
        Get-ChildItem -LiteralPath $appBackup -File | Copy-Item -Destination $InstallDirectory -Force
        if (Test-Path $startFile) { Start-Process -FilePath $startFile -WorkingDirectory $InstallDirectory -WindowStyle Hidden }
        throw
    }
}
catch {
    [System.Windows.Forms.MessageBox]::Show("تعذر تحديث AGIRE:`n$($_.Exception.Message)", 'تحديث AGIRE', [System.Windows.Forms.MessageBoxButtons]::OK, [System.Windows.Forms.MessageBoxIcon]::Error) | Out-Null
    exit 1
}
finally {
    if ($work -and (Test-Path $work)) { Remove-Item $work -Recurse -Force -ErrorAction SilentlyContinue }
}
