@echo off
setlocal
set "ASPNETCORE_ENVIRONMENT=Local"
set "DOTNET_GCServer=0"
set "DOTNET_TieredPGO=0"
set "ASPNETCORE_URLS=http://0.0.0.0:5182"
cd /d "%~dp0"
"%~dp0Hawdh.Portal.exe"
endlocal
