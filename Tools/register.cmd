@echo off
REM ============================================================
REM  AI SolidWorks Assistant - register the add-in (per-user)
REM  No admin rights needed (writes to HKCU).
REM
REM  Usage:  register.cmd [path\to\AiSolidWorksAssistant.dll]
REM ============================================================
setlocal

set "DLL=%~1"
if "%DLL%"=="" set "DLL=%~dp0..\build\AiSolidWorksAssistant.dll"
if not exist "%DLL%" set "DLL=%~dp0..\src\AiSolidWorksAssistant\bin\Release\net472\AiSolidWorksAssistant.dll"

if not exist "%DLL%" (
    echo [ERROR] AiSolidWorksAssistant.dll not found.
    echo Build the add-in first, or pass the DLL path:
    echo   register.cmd C:\path\to\AiSolidWorksAssistant.dll
    exit /b 1
)

echo Registering: %DLL%

"%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\RegAsm.exe" /codebase /nologo "%DLL%"
if errorlevel 1 (
    echo [ERROR] RegAsm failed.
    exit /b 1
)

set "KEY=HKCU\SOFTWARE\SolidWorks\Addins\{597151FC-6387-4AEE-B0F7-E0844B6E9F0F}"

reg add "%KEY%" /ve /d "AI SolidWorks Assistant" /f >nul
reg add "%KEY%" /v Description /d "AI assistant foundation - chat task pane, model context and validated CAD command pipeline (Phase 1)." /f >nul
reg add "%KEY%" /v Startup /t REG_DWORD /d 1 /f >nul

echo.
echo Done. Start SOLIDWORKS - the AI Assistant task pane should appear.
echo To load it manually: Tools ^> Add-Ins... ^> "AI SolidWorks Assistant"
endlocal
