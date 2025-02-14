# Roslyn Analyzer Demo

A minimal demo of custom Roslyn analyzers for team code conventions.

## Structure

- `DemoAnalyzers/`: Analyzer project (outputs NuGet package)
- `DemoProject/`: Sample project using the analyzers

## Quick Start

1. **Build Analyzers**

   ```bash
   cd DemoAnalyzers
   dotnet pack -c Release -o ../packages

   ```

2. **Add Local Package Source**

   ```bash
   cd DemoProject
   dotnet nuget add source ../packages -n local

   ```

3. **Use Analyzers**
   ```bash
   dotnet build
   ```
   Warnings/errors will appear for convention violations.

## CI/CD

Sample GitHub Actions workflow included (.github/workflows/build.yml).
