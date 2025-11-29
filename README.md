# Schema Diagram Viewer

A desktop application for visualizing SQL Server database schemas as interactive diagrams. Built with Blazor Server and ElectronNET.

## Features

- **Database Connection**: Connect to SQL Server using Windows or SQL authentication
- **Schema Visualization**: Interactive diagram showing tables, views, and relationships
- **Save/Load**: Save diagram layouts and reload them later
- **PDF Export**: Export diagrams to PDF with customizable page sizes
- **Drag & Drop**: Reposition tables and views on the canvas

## Requirements

- .NET 9.0 SDK
- SQL Server database access

## Building a Standalone Executable

### Option 1: Single-File Executable (Recommended - No Electron Needed!)

**Create a single .exe file with everything included:**

**Quick Build (Windows)**
```bash
build-single-file.bat
```

**Using Makefile**
```bash
make build-single
```

**Manual Build**
```bash
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:IncludeAllContentForSelfExtract=true /p:EnableCompressionInSingleFile=true
```

**Result:**
- Single file: `bin/Release/net9.0/win-x64/publish/SchemaDiagramViewer.exe`
- Contains **everything**: code, .NET runtime, all dependencies, static files
- **No other files needed** - just copy this one .exe anywhere and run it!
- When you run it, it starts on `http://localhost:5092` (opens in your browser)

**Note:** The single file will be large (typically 50-100MB) because it includes the entire .NET runtime. This is normal and expected.

### Option 2: Electron Desktop App (Optional)

If you want a native desktop window instead of opening in a browser:

**Prerequisites**
1. Install ElectronNET.CLI globally:
```bash
dotnet tool install ElectronNET.CLI -g
```

2. Initialize ElectronNET in your project (if not already done):
```bash
electronize init
```

**Quick Build (Windows)**
```bash
build-electron.bat
```

**Manual Build**
```bash
electronize build /target win /dotnet-configuration Release
```

The build creates a folder in `bin/Desktop/win-x64/` containing:
- `SchemaDiagramViewer.exe` - The main executable (double-click to run)
- Electron binaries and dependencies

### Running

**Development (Localhost - No Electron)**
```bash
run-localhost.bat
```
or
```bash
dotnet run --launch-profile http
```
Opens at `http://localhost:5092` in your browser.

**Development (Electron Desktop)**
```bash
dotnet run
```

**Production (Single-File)**
Double-click `SchemaDiagramViewer.exe` from `bin/Release/net9.0/win-x64/publish/`
- No installation needed
- No dependencies needed
- Just run the .exe file!

## Usage

1. Launch the application
2. Enter SQL Server connection details on the connection page
3. View and interact with the schema diagram
4. Use the toolbar to:
   - Refresh the schema
   - Save/load diagram layouts
   - Export to PDF
   - Configure export settings

## Technologies

- Blazor Server
- ElectronNET
- QuestPDF
- Microsoft.Data.SqlClient

