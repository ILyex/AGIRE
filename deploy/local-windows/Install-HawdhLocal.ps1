#requires -Version 5.1
[CmdletBinding()]
param(
    [int]$Port = 5182,
    [string]$InstallDirectory = 'C:\ProgramData\AGIRE\Hawdh'
)

$ErrorActionPreference = 'Stop'
$installerLog = Join-Path $env:TEMP 'HawdhInstaller.log'
try { Start-Transcript -Path $installerLog -Append | Out-Null } catch { }
Add-Type -AssemblyName System.Windows.Forms
[System.Windows.Forms.Application]::EnableVisualStyles()
trap {
    $details = $_.Exception.Message
    try { Stop-Transcript | Out-Null } catch { }
    [System.Windows.Forms.MessageBox]::Show("تعذر إكمال التثبيت.`n`n$details`n`nتم حفظ تفاصيل المشكلة في:`n$installerLog", 'منصة مهندس', [System.Windows.Forms.MessageBoxButtons]::OK, [System.Windows.Forms.MessageBoxIcon]::Error) | Out-Null
    exit 1
}

function Read-InstallerCredentials {
    $form = New-Object System.Windows.Forms.Form
    $form.Text = 'إعداد منصة مهندس'
    $form.Width = 460; $form.Height = 255
    $form.StartPosition = 'CenterScreen'; $form.RightToLeft = 'Yes'; $form.RightToLeftLayout = $true
    $form.FormBorderStyle = 'FixedDialog'; $form.MaximizeBox = $false; $form.MinimizeBox = $false
    $logoPath = Join-Path $packageDirectory 'agire-installer.png'
    if (-not (Test-Path $logoPath)) { $logoPath = Join-Path $packagePayload 'agire-installer.png' }
    if (Test-Path $logoPath) {
        $logo = New-Object System.Windows.Forms.PictureBox
        $logo.Image = [System.Drawing.Image]::FromFile($logoPath); $logo.SizeMode = 'Zoom'; $logo.Width = 52; $logo.Height = 52; $logo.Location = New-Object System.Drawing.Point(365, 10)
        $form.Controls.Add($logo)
    }
    $emailLabel = New-Object System.Windows.Forms.Label
    $emailLabel.Text = 'بريد مدير المنصة'; $emailLabel.AutoSize = $true; $emailLabel.Location = New-Object System.Drawing.Point(24, 18)
    $emailBox = New-Object System.Windows.Forms.TextBox
    $emailBox.Width = 390; $emailBox.Location = New-Object System.Drawing.Point(24, 43)
    $passwordLabel = New-Object System.Windows.Forms.Label
    $passwordLabel.Text = 'كلمة المرور (12 حرفاً على الأقل)'; $passwordLabel.AutoSize = $true; $passwordLabel.Location = New-Object System.Drawing.Point(24, 78)
    $passwordBox = New-Object System.Windows.Forms.TextBox
    $passwordBox.Width = 390; $passwordBox.Location = New-Object System.Drawing.Point(24, 103); $passwordBox.UseSystemPasswordChar = $true
    $ok = New-Object System.Windows.Forms.Button
    $ok.Text = 'بدء التثبيت'; $ok.Width = 105; $ok.Location = New-Object System.Drawing.Point(205, 155)
    $cancel = New-Object System.Windows.Forms.Button
    $cancel.Text = 'إلغاء'; $cancel.DialogResult = [System.Windows.Forms.DialogResult]::Cancel; $cancel.Width = 90; $cancel.Location = New-Object System.Drawing.Point(105, 155)
    $ok.Add_Click({
        $password = $passwordBox.Text
        $validPassword = $password.Length -ge 12 -and $password -cmatch '[a-z]' -and $password -cmatch '[A-Z]' -and $password -match '\d' -and $password -match '[^a-zA-Z0-9]'
        if ([string]::IsNullOrWhiteSpace($emailBox.Text) -or -not $validPassword) {
            [System.Windows.Forms.MessageBox]::Show('كلمة المرور يجب أن تحتوي على 12 حرفاً على الأقل، وحرف إنجليزي كبير، وحرف صغير، ورقم، ورمز مثل !.', 'بيانات غير مكتملة', [System.Windows.Forms.MessageBoxButtons]::OK, [System.Windows.Forms.MessageBoxIcon]::Warning) | Out-Null
            return
        }
        $form.DialogResult = [System.Windows.Forms.DialogResult]::OK
        $form.Close()
    })
    $form.Controls.AddRange(@($emailLabel, $emailBox, $passwordLabel, $passwordBox, $ok, $cancel)); $form.AcceptButton = $ok; $form.CancelButton = $cancel
    if ($form.ShowDialog() -ne [System.Windows.Forms.DialogResult]::OK) { throw 'تم إلغاء التثبيت.' }
    return [PSCustomObject]@{ Email = $emailBox.Text; Password = $passwordBox.Text }
}

function New-ProgressForm {
    $form = New-Object System.Windows.Forms.Form
    $form.Text = 'منصة مهندس'
    $form.Width = 440; $form.Height = 135
    $form.StartPosition = 'CenterScreen'; $form.RightToLeft = 'Yes'; $form.RightToLeftLayout = $true
    $form.FormBorderStyle = 'FixedDialog'; $form.ControlBox = $false
    $label = New-Object System.Windows.Forms.Label
    $label.Text = 'جاري تجهيز المنصة، يرجى الانتظار...'; $label.AutoSize = $true; $label.Location = New-Object System.Drawing.Point(32, 24)
    $bar = New-Object System.Windows.Forms.ProgressBar
    $bar.Style = 'Marquee'; $bar.MarqueeAnimationSpeed = 25; $bar.Width = 370; $bar.Location = New-Object System.Drawing.Point(32, 58)
    $form.Controls.AddRange(@($label, $bar)); $form.Show(); [System.Windows.Forms.Application]::DoEvents()
    return $form
}

function Test-Administrator {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = [Security.Principal.WindowsPrincipal]::new($identity)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

if (-not (Test-Administrator)) {
    $arguments = "-NoLogo -NoProfile -STA -ExecutionPolicy Bypass -File `"$($MyInvocation.MyCommand.Path)`" -Port $Port -InstallDirectory `"$InstallDirectory`""
    Start-Process powershell.exe -Verb RunAs -WindowStyle Normal -ArgumentList $arguments -ErrorAction Stop
    try { Stop-Transcript | Out-Null } catch { }
    exit 0
}

$scriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
$packageDirectory = if (Test-Path (Join-Path $scriptDirectory '..\payload')) { (Resolve-Path (Join-Path $scriptDirectory '..')).Path } else { $scriptDirectory }
$packagePayload = Join-Path $packageDirectory 'payload'
$launcher = Join-Path $packageDirectory 'AGIRE.exe'
$icon = Join-Path $packageDirectory 'agire.ico'
if (-not (Test-Path $packagePayload)) { $packagePayload = $packageDirectory }
if (-not (Test-Path $icon)) { $icon = Join-Path $packagePayload 'agire.ico' }
$exe = Join-Path $packagePayload 'Hawdh.Portal.exe'
if (-not (Test-Path $exe)) { throw "Hawdh.Portal.exe غير موجود بجانب ملف التثبيت." }

New-Item -ItemType Directory -Force -Path $InstallDirectory, (Join-Path $InstallDirectory 'data'), (Join-Path $InstallDirectory 'data\keys'), (Join-Path $InstallDirectory 'data\backups') | Out-Null
Get-ChildItem $packagePayload -File | ForEach-Object {
    Copy-Item $_.FullName (Join-Path $InstallDirectory $_.Name) -Force
}
if (Test-Path $launcher) { Copy-Item $launcher (Join-Path $InstallDirectory 'AGIRE.exe') -Force }
if (Test-Path $icon) { Copy-Item $icon (Join-Path $InstallDirectory 'agire.ico') -Force }
$updateScript = Join-Path $packageDirectory 'Update-AGIRE.ps1'
if (Test-Path $updateScript) { Copy-Item $updateScript (Join-Path $InstallDirectory 'Update-AGIRE.ps1') -Force }

$hostname = [Net.Dns]::GetHostName()
$ip = (Get-NetIPAddress -AddressFamily IPv4 -PrefixOrigin Dhcp -ErrorAction SilentlyContinue |
    Where-Object { $_.IPAddress -notlike '127.*' -and $_.IPAddress -notlike '169.254.*' } |
    Select-Object -First 1 -ExpandProperty IPAddress)
$allowedHosts = @('localhost', '127.0.0.1', $hostname)
if ($ip) { $allowedHosts += $ip }

$settings = [ordered]@{
    ConnectionStrings = [ordered]@{ DefaultConnection = 'Data Source=data/hawdh.local.db' }
    Database = [ordered]@{ Provider = 'Sqlite' }
    DataProtection = [ordered]@{ KeysPath = 'data/keys' }
    AllowedHosts = ($allowedHosts | Select-Object -Unique) -join ';'
    Logging = [ordered]@{ LogLevel = [ordered]@{ Default = 'Information'; 'Microsoft.AspNetCore' = 'Warning' } }
}
$settings | ConvertTo-Json -Depth 6 | Set-Content (Join-Path $InstallDirectory 'appsettings.Local.json') -Encoding UTF8

$credentials = Read-InstallerCredentials
$adminEmail = $credentials.Email
$adminPassword = $credentials.Password

[System.Windows.Forms.MessageBox]::Show('جاري إعداد المنصة لأول مرة. قد يستغرق ذلك أقل من دقيقة.', 'منصة مهندس', [System.Windows.Forms.MessageBoxButtons]::OK, [System.Windows.Forms.MessageBoxIcon]::Information) | Out-Null

# Stop an older installation before preparing the updated one.
if (Get-ScheduledTask -TaskName 'AGIRE Hawdh local platform' -ErrorAction SilentlyContinue) {
    Stop-ScheduledTask -TaskName 'AGIRE Hawdh local platform' -ErrorAction SilentlyContinue
    Unregister-ScheduledTask -TaskName 'AGIRE Hawdh local platform' -Confirm:$false -ErrorAction SilentlyContinue
}
Get-Process -Name 'Hawdh.Portal' -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Milliseconds 500

# Initialize the local SQLite database and create the first administrator once.
$env:ASPNETCORE_ENVIRONMENT = 'Local'
$env:ASPNETCORE_URLS = "http://127.0.0.1:$Port"
$env:SeedAdmin__Email = $adminEmail
$env:SeedAdmin__Password = $adminPassword
$progress = New-ProgressForm
$bootstrap = Start-Process -FilePath $exe -WorkingDirectory $InstallDirectory -PassThru -WindowStyle Hidden
try {
    $ready = $false
    1..20 | ForEach-Object {
        Start-Sleep -Seconds 1
        [System.Windows.Forms.Application]::DoEvents()
        if ($bootstrap.HasExited) { throw 'توقف تشغيل المنصة أثناء الإعداد. تحقق من بيانات المدير ثم أعد المحاولة.' }
        try { if ((Invoke-WebRequest "http://127.0.0.1:$Port/health" -UseBasicParsing -TimeoutSec 2).StatusCode -eq 200) { $ready = $true; break } } catch { }
    }
    if (-not $ready) { throw 'تعذر تشغيل المنصة أثناء التثبيت.' }
}
finally {
    # Stop the temporary bootstrap instance before starting the managed task.
    if ($bootstrap -and -not $bootstrap.HasExited) { Stop-Process -Id $bootstrap.Id -Force }
    if ($progress) { $progress.Close(); $progress.Dispose() }
    Remove-Item Env:SeedAdmin__Email, Env:SeedAdmin__Password -ErrorAction SilentlyContinue
}

$startFile = Join-Path $InstallDirectory 'Start-HawdhLocal.cmd'
@("@echo off", "setlocal", "set ASPNETCORE_ENVIRONMENT=Local", "set ASPNETCORE_URLS=http://0.0.0.0:$Port", "cd /d `"%~dp0`"", "start `"Hawdh Portal`" /min `"%~dp0Hawdh.Portal.exe`"", "endlocal") | Set-Content $startFile -Encoding ASCII

$firewallName = 'AGIRE Hawdh local platform'
if (-not (Get-NetFirewallRule -DisplayName $firewallName -ErrorAction SilentlyContinue)) {
    New-NetFirewallRule -DisplayName $firewallName -Direction Inbound -Protocol TCP -LocalPort $Port -Action Allow -Profile Private | Out-Null
}

$shell = New-Object -ComObject WScript.Shell
$shortcut = $shell.CreateShortcut((Join-Path ([Environment]::GetFolderPath('CommonDesktopDirectory')) 'AGIRE.lnk'))
$shortcut.TargetPath = $startFile
$shortcut.WorkingDirectory = $InstallDirectory
$shortcut.Description = 'تشغيل منصة مهندس على هذا الكمبيوتر'
$shortcut.IconLocation = "$(Join-Path $InstallDirectory 'AGIRE.exe'),0"
$shortcut.Save()

# Start automatically for the signed-in user as a reliable fallback to the SYSTEM task.
$startupShortcut = $shell.CreateShortcut((Join-Path ([Environment]::GetFolderPath('CommonStartup')) 'AGIRE.lnk'))
$startupShortcut.TargetPath = $startFile
$startupShortcut.WorkingDirectory = $InstallDirectory
$startupShortcut.Description = 'تشغيل منصة مهندس تلقائيا عند تسجيل الدخول'
$startupShortcut.IconLocation = "$(Join-Path $InstallDirectory 'AGIRE.exe'),0"
$startupShortcut.Save()

$updateShortcut = $shell.CreateShortcut((Join-Path ([Environment]::GetFolderPath('CommonDesktopDirectory')) 'تحديث AGIRE.lnk'))
$updateShortcut.TargetPath = (Join-Path $env:SystemRoot 'System32\WindowsPowerShell\v1.0\powershell.exe')
$updateShortcut.Arguments = "-NoProfile -ExecutionPolicy Bypass -File `"$(Join-Path $InstallDirectory 'Update-AGIRE.ps1')`" -InstallDirectory `"$InstallDirectory`""
$updateShortcut.WorkingDirectory = $InstallDirectory
$updateShortcut.Description = 'التحقق من تحديثات منصة AGIRE وتثبيتها'
$updateShortcut.IconLocation = "$(Join-Path $InstallDirectory 'AGIRE.exe'),0"
$updateShortcut.Save()

$url = if ($ip) { "http://${ip}:$Port" } else { "http://$hostname`:$Port" }
$urlShortcut = $shell.CreateShortcut((Join-Path ([Environment]::GetFolderPath('CommonDesktopDirectory')) 'فتح AGIRE.lnk'))
$urlShortcut.TargetPath = $url
$urlShortcut.Description = "فتح منصة مهندس - $url"
$urlShortcut.Save()

# Start the final instance directly now; this makes localhost available immediately.
Start-Process -FilePath $startFile -WorkingDirectory $InstallDirectory -WindowStyle Hidden | Out-Null

$principal = New-ScheduledTaskPrincipal -UserId 'SYSTEM' -LogonType ServiceAccount -RunLevel Highest

$started = $false
1..15 | ForEach-Object {
    Start-Sleep -Seconds 1
    try { if ((Invoke-WebRequest "http://127.0.0.1:$Port/health" -UseBasicParsing -TimeoutSec 2).StatusCode -eq 200) { $started = $true; break } } catch { }
}
if (-not $started) { throw 'تم التثبيت لكن تعذر تشغيل خدمة المنصة. افتح اختصار "منصة مهندس" مرة واحدة ثم أعد المحاولة.' }

$backupScript = Join-Path $InstallDirectory 'Backup-HawdhLocal.ps1'
$backupTaskName = 'AGIRE Hawdh daily backup'
if (Get-ScheduledTask -TaskName $backupTaskName -ErrorAction SilentlyContinue) { Unregister-ScheduledTask -TaskName $backupTaskName -Confirm:$false }
$backupAction = New-ScheduledTaskAction -Execute 'powershell.exe' -Argument "-NoProfile -ExecutionPolicy Bypass -File `"$backupScript`" -InstallDirectory `"$InstallDirectory`""
$backupTrigger = New-ScheduledTaskTrigger -Daily -At 2:00am
Register-ScheduledTask -TaskName $backupTaskName -Action $backupAction -Trigger $backupTrigger -Principal $principal -Description 'نسخة احتياطية يومية لقاعدة منصة مهندس' | Out-Null

$message = "تم تثبيت منصة مهندس بنجاح.`n`nالرابط على هذا الجهاز: http://localhost:$Port"
if ($ip) { $message += "`nالرابط من جهاز آخر على نفس الشبكة: $url" }
$message += '`n`nتم إنشاء اختصارات التشغيل والفتح على سطح المكتب.'
[System.Windows.Forms.MessageBox]::Show($message, 'منصة مهندس', [System.Windows.Forms.MessageBoxButtons]::OK, [System.Windows.Forms.MessageBoxIcon]::Information) | Out-Null
try { Stop-Transcript | Out-Null } catch { }
