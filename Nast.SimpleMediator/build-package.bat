@echo off
echo SimpleMediator NuGet Package Builder
echo ====================================

echo Cleaning previous builds...
if exist bin rmdir /s /q bin
if exist obj rmdir /s /q obj

echo Restoring NuGet packages...
dotnet restore Nast.SimpleMediator.csproj

echo Building project...
dotnet build Nast.SimpleMediator.csproj --configuration Release --no-restore

echo Creating NuGet package...
dotnet pack Nast.SimpleMediator.csproj --configuration Release --no-build --output bin\Release

echo.
echo Package created successfully!
echo.
echo To publish to NuGet, run:
echo dotnet nuget push "bin\Release\*.nupkg" --api-key YOUR_API_KEY --source https://api.nuget.org/v3/index.json
echo.
pause
