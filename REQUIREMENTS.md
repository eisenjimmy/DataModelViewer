# Requirements to Run Schema Diagram Viewer

## Quick Answer

**To run on localhost (development):**
- ✅ **.NET 9.0 SDK** (download from https://dotnet.microsoft.com/download)
- ✅ **All source files** in the project
- ❌ **NOT needed**: `bin/` and `obj/` folders (these are generated)
- ❌ **NOT needed**: Electron (only needed for desktop app packaging)

## Detailed Requirements

### For Development (Running on localhost)

**Required:**
1. **.NET 9.0 SDK** - Install from https://dotnet.microsoft.com/download/dotnet/9.0
   - Verify installation: `dotnet --version` (should show 9.x.x)

2. **Source Files** - You need these folders/files:
   ```
   ✅ Pages/          - All .razor page files
   ✅ Services/       - All service classes
   ✅ Models/         - Data models
   ✅ Shared/         - Shared components
   ✅ wwwroot/        - Static files (CSS, JS, images)
   ✅ Program.cs      - Main entry point
   ✅ SchemaDiagramViewer.csproj - Project file
   ✅ appsettings.json - Configuration
   ✅ Properties/launchSettings.json - Launch profiles
   ✅ NativeMethods.cs - P/Invoke helpers
   ✅ electron.manifest.json - Electron config (optional, but won't hurt)
   ```

**NOT Required (Generated):**
- ❌ `bin/` folder - Generated during build
- ❌ `obj/` folder - Generated during build
- ❌ Electron binaries - Only needed for desktop packaging

### Running the App

**Option 1: Simple batch file**
```bash
run-localhost.bat
```

**Option 2: Makefile** (if you have `make` installed)
```bash
make run
```

**Option 3: Direct dotnet command**
```bash
dotnet run --launch-profile http
```

The app will:
- Start on **http://localhost:5092**
- Automatically open in your browser
- Run as a Blazor Server app (no Electron needed)

### What Gets Generated

When you run `dotnet run` or `dotnet build`:
- `bin/` folder - Contains compiled DLLs and executables
- `obj/` folder - Contains intermediate build files
- These are **automatically created** - you don't need to include them

### For Production (Single-File Executable)

**YES! You can create a single .exe file with everything included!**

**Option 1: Using the batch file (Windows)**
```bash
build-single-file.bat
```

**Option 2: Using Makefile**
```bash
make build-single
```

**Option 3: Direct command**
```bash
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:IncludeAllContentForSelfExtract=true /p:EnableCompressionInSingleFile=true
```

**Result:**
- Single file: `bin/Release/net9.0/win-x64/publish/SchemaDiagramViewer.exe`
- Contains **everything**: code, .NET runtime, all dependencies, static files (wwwroot)
- **No other files needed** - just copy this one .exe anywhere and run it!
- When you run it, it starts on `http://localhost:5092`

**Note:** The single file will be large (typically 50-100MB) because it includes the entire .NET runtime. This is normal and expected for self-contained applications.

## File Structure Summary

**Essential Files (must have):**
- All `.cs` files (C# source code)
- All `.razor` files (Blazor components)
- All `.csproj` files (project configuration)
- `wwwroot/` folder (static web assets)
- `appsettings.json` (configuration)
- `Properties/launchSettings.json` (launch profiles)

**Optional Files:**
- `electron.manifest.json` - Only needed for Electron packaging
- `build-electron.bat` - Only needed for Electron builds
- `Makefile` - Convenience script (not required)

**Generated Files (don't need):**
- `bin/` - Build output
- `obj/` - Intermediate files
- Any `.dll`, `.exe`, `.pdb` files in bin/

## Minimum Setup

If you're starting fresh, you need:
1. Install .NET 9.0 SDK
2. Clone/copy the source files (excluding `bin/` and `obj/`)
3. Run `dotnet restore` (downloads NuGet packages)
4. Run `dotnet run --launch-profile http`

That's it! The app will work on localhost without Electron.

