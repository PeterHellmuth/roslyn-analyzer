# Roslyn Analyzer Demo

A minimal demo of custom Roslyn analyzers for team code conventions.

## Structure

- `DemoAnalyzers/`: Analyzer project (outputs NuGet package)
- `DemoProject/`: Sample project using the analyzers

## Quick Start

**Prerequisites**

Enable ".NET Compiler Platform SDK" in Visual Studio Installer under "Individual Components"

1. **Build and Package Analyzers**

   ```bash
   cd DemoAnalyzers
   dotnet build -c Release
   dotnet pack -c Release -o ../packages

   ```

2. **Add Local Package Source**

   ```bash
   cd ../DemoProject
   dotnet nuget add source ../packages -n local

   ```

3. **Use Analyzers**
   ```bash
   dotnet build
   ```
   Warnings/errors will appear for convention violations.

## CI/CD

Sample GitHub Actions workflow included (.github/workflows/build.yml).
