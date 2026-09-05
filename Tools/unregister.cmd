@echo off
REM ============================================================
REM  AI SolidWorks Assistant - unregister the add-in (per-user)
REM
REM  Usage:  unregister.cmd [path\to\AiSolidWorksAssistant.dll]
REM ============================================================
setlocal

set "DLL=%~1"
if "%DLL%"=="" set "DLL=%~dp0..\build\AiSolidWorksAssistant.dll"
if not exist "%DLL%" set "DLL=%~dp0..\src\AiSolidWorksAssistant\bin\Release\net472\AiSolidWorksAssistant.dll"

if exist "%DLL%" (
    echo Unregistering: %DLL%
    "%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\RegAsm.exe" /u /nologo "%DLL%"
)

set "KEY=HKCU\SOFTWARE\SolidWorks\Addins\{597151FC-6387-4AEE-B0F7-E0844B6E9F0F}"
reg delete "%KEY%" /f >nul 2>&1

echo Done. The add-in will no longer load in SOLIDWORKS.
endlocal
