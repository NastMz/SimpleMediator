# Publishing Guide for Nast.SimpleMediator

This guide explains how to build and publish the SimpleMediator NuGet package.

## Prerequisites

- .NET 8 SDK installed
- NuGet CLI tools (included with .NET SDK)
- NuGet account and API key (for publishing)

## Building the Package

### Option 1: Using PowerShell Script (Recommended)

```powershell
# Build package only
.\build-package.ps1

# Build and publish to NuGet
.\build-package.ps1 -PublishToNuGet -ApiKey "YOUR_NUGET_API_KEY"

# Build with specific version
.\build-package.ps1 -Version "1.1.0"

# Clean build
.\build-package.ps1 -Clean
```

### Option 2: Using Batch File

```batch
build-package.bat
```

### Option 3: Manual Commands

```bash
# Restore packages
dotnet restore Nast.SimpleMediator.csproj

# Build the project
dotnet build Nast.SimpleMediator.csproj --configuration Release

# Create NuGet package
dotnet pack Nast.SimpleMediator.csproj --configuration Release --output ./bin/Release
```

## Publishing to NuGet

### Manual Publishing

1. Build the package using one of the methods above
2. Get your NuGet API key from [nuget.org](https://www.nuget.org/account/apikeys)
3. Run the publish command:

```bash
dotnet nuget push "bin/Release/*.nupkg" --api-key YOUR_API_KEY --source https://api.nuget.org/v3/index.json
```

### Automated Publishing with GitHub Actions

1. Add your NuGet API key as a GitHub secret named `NUGET_API_KEY`
2. Push a tag with version format (e.g., `v1.0.0`):

```bash
git tag v1.0.0
git push origin v1.0.0
```

The GitHub Actions workflow will automatically build and publish the package.

## Version Management

Update the version in the project file before building:

```xml
<Version>1.0.1</Version>
```

Or use the PowerShell script with the `-Version` parameter.

## Package Contents

The NuGet package includes:

- Main library DLL (`Nast.SimpleMediator.dll`)
- XML documentation (`Nast.SimpleMediator.xml`)
- Debug symbols (`Nast.SimpleMediator.pdb`)
- README.md
- LICENSE file

## Testing Before Publishing

Always test your package locally before publishing:

1. Build the package
2. Install it in a test project:
   ```bash
   dotnet add package Nast.SimpleMediator --source ./bin/Release
   ```
3. Verify functionality

## Package Metadata

Key package information is defined in `Nast.SimpleMediator.csproj`:

- Package ID: `Nast.SimpleMediator`
- Target Framework: `.NET 8.0`
- License: MIT
- Dependencies: `Microsoft.Extensions.DependencyInjection.Abstractions`

## Troubleshooting

### Common Issues

1. **Build Errors**: Ensure .NET 8 SDK is installed
2. **Missing Dependencies**: Run `dotnet restore` first
3. **Publishing Errors**: Verify API key and network connectivity
4. **Version Conflicts**: Increment version number for new releases

### Package Validation

Before publishing, the package is validated for:

- Proper metadata
- Required dependencies
- Documentation completeness
- Symbol availability
