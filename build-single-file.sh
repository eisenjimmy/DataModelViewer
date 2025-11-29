#!/bin/bash

echo "========================================"
echo "Building Single-File Executable"
echo "========================================"
echo ""

# Clean previous builds
echo "Cleaning previous builds..."
dotnet clean -c Release
if [ $? -ne 0 ]; then
    echo "Error: Failed to clean project"
    exit 1
fi

echo ""
echo "Building single-file executable..."
echo "This may take a few minutes..."
echo ""

# Detect OS and set runtime identifier
if [[ "$OSTYPE" == "linux-gnu"* ]]; then
    RID="linux-x64"
elif [[ "$OSTYPE" == "darwin"* ]]; then
    RID="osx-x64"
elif [[ "$OSTYPE" == "msys" || "$OSTYPE" == "win32" ]]; then
    RID="win-x64"
else
    echo "Unknown OS type: $OSTYPE"
    exit 1
fi

# Build single-file executable
dotnet publish -c Release -r $RID --self-contained true \
    /p:PublishSingleFile=true \
    /p:IncludeAllContentForSelfExtract=true \
    /p:EnableCompressionInSingleFile=true

if [ $? -ne 0 ]; then
    echo ""
    echo "Error: Build failed!"
    exit 1
fi

echo ""
echo "========================================"
echo "Build Complete!"
echo "========================================"
echo ""
echo "Your single-file executable is located at:"
echo "  bin/Release/net9.0/$RID/publish/SchemaDiagramViewer"
echo ""
echo "This single file contains:"
echo "  - All application code"
echo "  - All dependencies (.NET runtime, libraries)"
echo "  - All static web assets (wwwroot files)"
echo "  - Everything needed to run!"
echo ""
echo "You can copy just this file anywhere and run it!"
echo "It will start on http://localhost:5092"
echo ""

