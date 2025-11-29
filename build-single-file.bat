@echo off
echo ========================================
echo Building Single-File Executable
echo ========================================
echo.

REM Clean previous builds
echo Cleaning previous builds...
dotnet clean -c Release
if errorlevel 1 (
    echo Error: Failed to clean project
    pause
    exit /b 1
)

echo.
echo Building single-file executable...
echo This may take a few minutes...
echo.

REM Build single-file executable
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:IncludeAllContentForSelfExtract=true /p:EnableCompressionInSingleFile=true

if errorlevel 1 (
    echo.
    echo Error: Build failed!
    pause
    exit /b 1
)

echo.
echo ========================================
echo Build Complete!
echo ========================================
echo.
echo Your single-file executable is located at:
echo   bin\Release\net9.0\win-x64\publish\SchemaDiagramViewer.exe
echo.
echo This single .exe file contains:
echo   - All application code
echo   - All dependencies (.NET runtime, libraries)
echo   - All static web assets (wwwroot files)
echo   - Everything needed to run!
echo.
echo File size: 
for %%A in ("bin\Release\net9.0\win-x64\publish\SchemaDiagramViewer.exe") do echo   %%~zA bytes (%%~zA / 1048576 MB)
echo.
echo You can copy just this .exe file anywhere and run it!
echo It will start on http://localhost:5092
echo.
pause

