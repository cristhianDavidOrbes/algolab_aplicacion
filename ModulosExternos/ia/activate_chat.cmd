@echo off
setlocal

set "ALGOLAB_IA_ROOT=%~dp0"
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%ALGOLAB_IA_ROOT%iniciar_algolab_ia.ps1" %*
set "ALGOLAB_IA_EXIT=%ERRORLEVEL%"

if not "%ALGOLAB_IA_EXIT%"=="0" (
    echo.
    echo No se pudo activar la inteligencia artificial de AlgoLab.
    echo Revisa los mensajes anteriores para conocer la causa.
)

exit /b %ALGOLAB_IA_EXIT%
