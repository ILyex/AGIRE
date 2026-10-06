@echo off
setlocal
set "ASPNETCORE_ENVIRONMENT=Local"
set "ASPNETCORE_URLS=http://0.0.0.0:5182"
cd /d "%~dp0"
"%~dp0Hawdh.Portal.exe"
endlocal
