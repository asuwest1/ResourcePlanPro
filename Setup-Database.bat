@echo off
REM ResourcePlan Pro - Database Setup Script
REM Quick database creation for Windows environments

echo ============================================
echo ResourcePlan Pro - Database Setup
echo ============================================
echo.

REM Check if running as administrator
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo ERROR: This script must be run as Administrator
    echo Right-click and select "Run as administrator"
    pause
    exit /b 1
)

REM Get SQL Server instance
set /p SQLSERVER="Enter SQL Server instance (default: localhost): " || set SQLSERVER=localhost
if "%SQLSERVER%"=="" set SQLSERVER=localhost

REM Get database name
set /p DBNAME="Enter database name (default: ResourcePlanPro): " || set DBNAME=ResourcePlanPro
if "%DBNAME%"=="" set DBNAME=ResourcePlanPro

echo.
echo SQL Server: %SQLSERVER%
echo Database: %DBNAME%
echo.
echo Creating database...
echo.

REM Create database
echo [1/3] Creating database schema...
sqlcmd -S %SQLSERVER% -E -i "Database\01_CreateDatabase.sql" -v DatabaseName=%DBNAME%
if %errorLevel% neq 0 (
    echo ERROR: Failed to create database schema
    pause
    exit /b 1
)
echo   √ Database schema created

REM Load sample data
echo [2/3] Loading sample data...
sqlcmd -S %SQLSERVER% -E -d %DBNAME% -i "Database\02_SampleData.sql"
if %errorLevel% neq 0 (
    echo ERROR: Failed to load sample data
    pause
    exit /b 1
)
echo   √ Sample data loaded

REM Create views and procedures
echo [3/3] Creating views and stored procedures...
sqlcmd -S %SQLSERVER% -E -d %DBNAME% -i "Database\03_ViewsAndProcedures.sql"
if %errorLevel% neq 0 (
    echo ERROR: Failed to create views and procedures
    pause
    exit /b 1
)
echo   √ Views and procedures created

echo.
echo ============================================
echo Database Setup Complete!
echo ============================================
echo.
echo Database: %DBNAME%
echo Server: %SQLSERVER%
echo.
echo Demo Login:
echo   Username: jsmith
echo   Password: Password123!
echo.
echo Next Steps:
echo   1. Update Backend\appsettings.json with your connection string
echo   2. Run: cd Backend
echo   3. Run: dotnet run
echo   4. Navigate to Frontend in browser
echo.
pause
