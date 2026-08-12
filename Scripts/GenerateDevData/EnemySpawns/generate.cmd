@echo off
setlocal

set "SCRIPT_ROOT=%~dp0"
set "PROJECT_PATH=%SCRIPT_ROOT%EnemySpawnDataGenerator.csproj"
set "CONFIG_PATH=%SCRIPT_ROOT%NuGet.Config"
set "TOOL_STATE=%SCRIPT_ROOT%.tool-state"
set "APPDATA=%TOOL_STATE%\appdata"
set "NUGET_PACKAGES=%TOOL_STATE%\packages"
set "DOTNET_CLI_HOME=%TOOL_STATE%\dotnet-cli"
set "DOTNET_CLI_TELEMETRY_OPTOUT=1"
set "DOTNET_ADD_GLOBAL_TOOLS_TO_PATH=0"
set "DOTNET_NOLOGO=1"
set "DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1"

if not exist "%APPDATA%" mkdir "%APPDATA%"
if not exist "%NUGET_PACKAGES%" mkdir "%NUGET_PACKAGES%"
if not exist "%DOTNET_CLI_HOME%" mkdir "%DOTNET_CLI_HOME%"

dotnet restore "%PROJECT_PATH%" --configfile "%CONFIG_PATH%"
if errorlevel 1 exit /b %errorlevel%

if /I "%~1"=="--check" (
    dotnet run --project "%PROJECT_PATH%" --no-restore -- --check
) else (
    dotnet run --project "%PROJECT_PATH%" --no-restore
)
exit /b %errorlevel%
