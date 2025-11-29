@echo off
echo Starting Schema Diagram Viewer on localhost (HTTPS)...
echo.
echo The application will be available at:
echo   HTTPS: https://localhost:7259
echo   HTTP:  http://localhost:5092
echo.
echo Press Ctrl+C to stop the server.
echo.

dotnet run --launch-profile https

pause

