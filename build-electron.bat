@echo off
echo Building Schema Diagram Viewer as Electron app...
echo.

REM Install ElectronNET.CLI if not already installed
dotnet tool list -g | findstr ElectronNET.CLI >nul
if errorlevel 1 (
    echo Installing ElectronNET.CLI...
    dotnet tool install ElectronNET.CLI -g
)

REM Initialize ElectronNET if manifest doesn't exist
if not exist "electron.manifest.json" (
    echo Initializing ElectronNET...
    electronize init
)

REM Clean previous builds
echo Cleaning previous builds...
dotnet clean -c Release

REM Build the Electron app
echo Building Electron app...
electronize build /target win /dotnet-configuration Release

echo.
echo Build complete! Check bin/Desktop/win-x64/ for SchemaDiagramViewer.exe
echo.
echo The executable will:
echo - Hide the console window automatically
echo - Open a native Electron window with your app
echo - Include all static files (wwwroot)
echo.
pause

