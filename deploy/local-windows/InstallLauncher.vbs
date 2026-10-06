Option Explicit
Dim shell, fso, scriptPath, command, result
Set shell = CreateObject("WScript.Shell")
Set fso = CreateObject("Scripting.FileSystemObject")
scriptPath = fso.BuildPath(fso.GetParentFolderName(WScript.ScriptFullName), "Install-HawdhLocal.ps1")
command = "powershell.exe -NoLogo -NoProfile -STA -ExecutionPolicy Bypass -File """ & scriptPath & """"
result = shell.Run(command, 0, False)
If result <> 0 Then
  MsgBox "لم يكتمل بدء التثبيت. أعد المحاولة، وإذا تكررت المشكلة أرسل ملف HawdhInstaller.log من مجلد Temp للدعم.", vbExclamation, "منصة مهندس"
End If
