@echo off
echo === DisplaySwitcher Build ===
echo.
dotnet publish DisplaySwitcher\DisplaySwitcher.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true -o Build
echo.
if %ERRORLEVEL% EQU 0 (
    echo Build erfolgreich! Die EXE liegt im Ordner Build\
) else (
    echo Build fehlgeschlagen!
)
pause
