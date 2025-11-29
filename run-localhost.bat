@echo off
echo Starting Schema Diagram Viewer on localhost...
echo.
echo The application will be available at:
echo   HTTP:  http://localhost:5092
echo   HTTPS: https://localhost:7259
echo.
echo Press Ctrl+C to stop the server.
echo.

dotnet run --launch-profile http

pause

