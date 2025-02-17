# Roslyn Analyzer Demo

A minimal demo of custom Roslyn analyzers for team code conventions.

## Structure

- `DemoAnalyzers/`: Analyzer project (outputs NuGet package)
- `DemoProject/`: Sample project using the analyzers

## Prerequisites

- .NET 9.0
- Enable ".NET Compiler Platform SDK" in Visual Studio Installer under "Individual Components"

## Quick Start

Run ./package.sh in project folder.

## Manual build

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

## Resources

- [.NET Compiler Platform SDK Documentation](https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/)
- [Microsoft - Write your first analyzer and code fix tutorial](https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/tutorials/how-to-write-csharp-analyzer-code-fix)
- [Roslynator](https://github.com/dotnet/roslynator)
