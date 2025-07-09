# SimpleMediator NuGet Package Build and Publish Script
# This script helps build and publish the SimpleMediator NuGet package

param(
    [Parameter(Mandatory=$false)]
    [string]$Version = "1.0.0",
    
    [Parameter(Mandatory=$false)]
    [string]$Configuration = "Release",
    
    [Parameter(Mandatory=$false)]
    [string]$ApiKey = "",
    
    [Parameter(Mandatory=$false)]
    [string]$Source = "https://api.nuget.org/v3/index.json",
    
    [Parameter(Mandatory=$false)]
    [switch]$PublishToNuGet = $false,
    
    [Parameter(Mandatory=$false)]
    [switch]$Clean = $false,
    
    [Parameter(Mandatory=$false)]
    [switch]$Interactive = $false
)

$ErrorActionPreference = "Stop"
$ProjectPath = "Nast.SimpleMediator.csproj"
$OutputPath = "bin\$Configuration\net8.0"
$PackageOutputPath = "bin\$Configuration"

Write-Host "SimpleMediator NuGet Package Builder" -ForegroundColor Green
Write-Host "=====================================" -ForegroundColor Green

# Interactive mode for manual execution
if ($Interactive) {
    Write-Host ""
    Write-Host "🚀 Interactive Mode" -ForegroundColor Cyan
    Write-Host "This will guide you through the package building process." -ForegroundColor Gray
    Write-Host ""
    
    if ([string]::IsNullOrEmpty($Version) -or $Version -eq "1.0.0") {
        $Version = Read-Host "Enter version (current: 1.0.0)"
        if ([string]::IsNullOrEmpty($Version)) { $Version = "1.0.0" }
    }
    
    $configChoice = Read-Host "Build configuration (Release/Debug) [Release]"
    if (![string]::IsNullOrEmpty($configChoice)) { $Configuration = $configChoice }
    
    $cleanChoice = Read-Host "Clean previous builds? (y/N)"
    if ($cleanChoice -eq "y" -or $cleanChoice -eq "Y") { $Clean = $true }
    
    $publishChoice = Read-Host "Publish to NuGet after building? (y/N)"
    if ($publishChoice -eq "y" -or $publishChoice -eq "Y") { 
        $PublishToNuGet = $true
        if ([string]::IsNullOrEmpty($ApiKey)) {
            $ApiKey = Read-Host "Enter your NuGet API key" -AsSecureString
            $ApiKey = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($ApiKey))
        }
    }
    
    Write-Host ""
    Write-Host "Configuration Summary:" -ForegroundColor Yellow
    Write-Host "  Version: $Version" -ForegroundColor Gray
    Write-Host "  Configuration: $Configuration" -ForegroundColor Gray
    Write-Host "  Clean: $Clean" -ForegroundColor Gray
    Write-Host "  Publish: $PublishToNuGet" -ForegroundColor Gray
    Write-Host ""
    
    $confirm = Read-Host "Continue with these settings? (Y/n)"
    if ($confirm -eq "n" -or $confirm -eq "N") {
        Write-Host "Build cancelled." -ForegroundColor Yellow
        exit 0
    }
}

# Clean if requested
if ($Clean) {
    Write-Host "Cleaning previous builds..." -ForegroundColor Yellow
    if (Test-Path "bin") { Remove-Item "bin" -Recurse -Force }
    if (Test-Path "obj") { Remove-Item "obj" -Recurse -Force }
}

# Update version in project file if provided
if ($Version -ne "1.0.0") {
    Write-Host "Updating version to $Version..." -ForegroundColor Yellow
    $projectContent = Get-Content $ProjectPath -Raw
    $projectContent = $projectContent -replace '<Version>.*</Version>', "<Version>$Version</Version>"
    Set-Content $ProjectPath -Value $projectContent
}

# Restore packages
Write-Host "Restoring NuGet packages..." -ForegroundColor Yellow
dotnet restore $ProjectPath

# Build the project
Write-Host "Building project in $Configuration configuration..." -ForegroundColor Yellow
dotnet build $ProjectPath --configuration $Configuration --no-restore

# Run tests if they exist
$TestProject = "../Tests/Nast.SimpleMediator.Tests.csproj"
if (Test-Path $TestProject) {
    Write-Host "Running tests..." -ForegroundColor Yellow
    dotnet test $TestProject --configuration $Configuration --no-build --verbosity normal
}

# Pack the project
Write-Host "Creating NuGet package..." -ForegroundColor Yellow
dotnet pack $ProjectPath --configuration $Configuration --no-build --output $PackageOutputPath

$PackageFile = Get-ChildItem "$PackageOutputPath\*.nupkg" | Sort-Object LastWriteTime -Descending | Select-Object -First 1

if ($PackageFile) {
    Write-Host "Package created successfully: $($PackageFile.Name)" -ForegroundColor Green
    Write-Host "Package location: $($PackageFile.FullName)" -ForegroundColor Gray
    
    # Publish to NuGet if requested
    if ($PublishToNuGet) {
        if ([string]::IsNullOrEmpty($ApiKey)) {
            Write-Host "API Key is required for publishing to NuGet" -ForegroundColor Red
            exit 1
        }
        
        Write-Host "Publishing to NuGet..." -ForegroundColor Yellow
        dotnet nuget push $PackageFile.FullName --api-key $ApiKey --source $Source
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "Package published successfully!" -ForegroundColor Green
        } else {
            Write-Host "Failed to publish package" -ForegroundColor Red
            exit 1
        }
    } else {
        Write-Host ""
        Write-Host "To publish this package to NuGet, run:" -ForegroundColor Cyan
        Write-Host "dotnet nuget push `"$($PackageFile.FullName)`" --api-key YOUR_API_KEY --source https://api.nuget.org/v3/index.json" -ForegroundColor Gray
        Write-Host ""
        Write-Host "Or use this script with -PublishToNuGet -ApiKey YOUR_API_KEY" -ForegroundColor Gray
    }
} else {
    Write-Host "Failed to create package" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Build completed successfully!" -ForegroundColor Green
