# Publishing OpenRouter.NET to NuGet

This guide explains how to publish the OpenRouter.NET package to NuGet.

## Prerequisites

1. **NuGet Account**: Create an account at [nuget.org](https://www.nuget.org/)
2. **API Key**: Generate an API key from your NuGet account settings
3. **.NET SDK**: Ensure you have .NET 8.0 or 9.0 SDK installed

## Publishing Steps

### 1. Update Version Number

Edit `OpenRouter.NET.csproj` and increment the version:

```xml
<Version>0.3.2</Version>
```

Follow [Semantic Versioning](https://semver.org/):
- **Major** (1.0.0): Breaking changes
- **Minor** (0.3.0): New features, backward compatible
- **Patch** (0.3.1): Bug fixes

### 2. Clean Previous Builds

```bash
cd packages/dotnet-sdk
dotnet clean
rm -rf bin/ obj/
```

### 3. Build in Release Mode

```bash
dotnet build -c Release
```

Verify the build succeeds without errors.

### 4. Run Tests (Optional but Recommended)

```bash
cd ../../tests/OpenRouter.NET.Tests
dotnet test
```

### 5. Pack the NuGet Package

```bash
cd ../../packages/dotnet-sdk
dotnet pack -c Release -o ./nupkg
```

This creates a `.nupkg` file in the `nupkg` directory.

### 6. Publish to NuGet

```bash
dotnet nuget push ./nupkg/OpenRouter.NET.0.3.2.nupkg \
  --api-key YOUR_API_KEY \
  --source https://api.nuget.org/v3/index.json
```

Replace:
- `0.4.1` with your actual version number
- `YOUR_NUGET_API_KEY` with your actual NuGet API key

### 7. Verify Publication

1. Visit https://www.nuget.org/packages/OpenRouter.NET
2. Check that the new version appears (may take 5-10 minutes)
3. Test installation: `dotnet add package OpenRouter.NET --version 0.3.2`

## Best Practices

### Before Publishing

- [ ] Update version in `OpenRouter.NET.csproj`
- [ ] Update `README.md` with any new features
- [ ] Run all tests successfully
- [ ] Build in Release configuration
- [ ] Review package metadata (description, tags, etc.)
- [ ] Check that XML documentation is generated

### After Publishing

- [ ] Create a Git tag for the version
- [ ] Push tag to GitHub: `git tag v0.3.2 && git push origin v0.3.2`
- [ ] Create a GitHub Release with changelog
- [ ] Test installation in a fresh project

## Package Metadata

Key metadata fields in `OpenRouter.NET.csproj`:

```xml
<PackageId>OpenRouter.NET</PackageId>
<Version>0.3.2</Version>
<Authors>William Holmberg</Authors>
<Description>A .NET SDK for OpenRouter API - Unified interface for LLM APIs</Description>
<PackageTags>openrouter;llm;ai;sdk;api;gpt;claude;anthropic</PackageTags>
<PackageLicenseExpression>MIT</PackageLicenseExpression>
<PackageProjectUrl>https://github.com/williamholmberg/OpenRouter.NET</PackageProjectUrl>
<RepositoryUrl>https://github.com/williamholmberg/OpenRouter.NET</RepositoryUrl>
```

## Troubleshooting

### "Package already exists"
- Increment the version number
- NuGet doesn't allow overwriting published versions

### "Build failed"
- Check for compilation errors
- Ensure all dependencies are restored: `dotnet restore`
- Verify multi-targeting works: `dotnet build -f net8.0` and `dotnet build -f net9.0`

### "Missing dependencies"
- Ensure all PackageReference entries are correct
- Check framework references (Microsoft.AspNetCore.App)

## CI/CD Publishing (Optional)

For automated publishing via GitHub Actions:

```yaml
name: Publish NuGet

on:
  push:
    tags:
      - 'v*'

jobs:
  publish:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0.x'
      
      - name: Restore
        run: dotnet restore packages/dotnet-sdk
      
      - name: Build
        run: dotnet build packages/dotnet-sdk -c Release --no-restore
      
      - name: Pack
        run: dotnet pack packages/dotnet-sdk -c Release --no-build -o ./nupkg
      
      - name: Publish
        run: dotnet nuget push ./nupkg/*.nupkg --api-key ${{ secrets.NUGET_API_KEY }} --source https://api.nuget.org/v3/index.json
```

Store your NuGet API key in GitHub repository secrets as `NUGET_API_KEY`.

## Quick Reference

```bash
# Full publishing workflow
cd packages/dotnet-sdk
dotnet clean
dotnet restore
dotnet build -c Release
dotnet pack -c Release -o ./nupkg
dotnet nuget push ./nupkg/OpenRouter.NET.*.nupkg --api-key YOUR_KEY --source https://api.nuget.org/v3/index.json

# Create git tag
git tag v0.3.2
git push origin v0.3.2
```

## Support

- **Issues**: https://github.com/williamholmberg/OpenRouter.NET/issues
- **NuGet**: https://www.nuget.org/packages/OpenRouter.NET

