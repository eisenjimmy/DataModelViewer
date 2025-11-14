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

## Running

### Desktop App (Electron)
```bash
dotnet run
```

### Web App
```bash
dotnet run --no-electron
```

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

