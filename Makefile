.PHONY: run run-http run-https build clean help

# Default target
.DEFAULT_GOAL := help

# Run the application on HTTP (localhost:5092)
run: run-http

# Run on HTTP only
run-http:
	@echo Starting Schema Diagram Viewer on http://localhost:5092...
	dotnet run --launch-profile http

# Run on HTTPS (with HTTP fallback)
run-https:
	@echo Starting Schema Diagram Viewer on https://localhost:7259...
	dotnet run --launch-profile https

# Build the project
build:
	@echo Building Schema Diagram Viewer...
	dotnet build

# Build single-file executable (Windows)
build-single:
	@echo Building single-file executable for Windows...
	dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:IncludeAllContentForSelfExtract=true /p:EnableCompressionInSingleFile=true
	@echo Build complete! Executable: bin/Release/net9.0/win-x64/publish/SchemaDiagramViewer.exe

# Clean build artifacts
clean:
	@echo Cleaning build artifacts...
	dotnet clean

# Show help
help:
	@echo Schema Diagram Viewer - Makefile Commands
	@echo.
	@echo Available targets:
	@echo   make run           - Run on HTTP (localhost:5092) - default
	@echo   make run-http      - Run on HTTP only (localhost:5092)
	@echo   make run-https     - Run on HTTPS (localhost:7259) with HTTP fallback
	@echo   make build         - Build the project
	@echo   make build-single  - Build single-file executable (Windows)
	@echo   make clean         - Clean build artifacts
	@echo   make help          - Show this help message
	@echo.
	@echo Note: On Windows, you may need to install 'make' first.
	@echo   - Via Chocolatey: choco install make
	@echo   - Via Git for Windows (includes make)
	@echo   - Or use run-localhost.bat instead

